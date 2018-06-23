#include <string>
#include <string_view>
#include <vector>

#include "CCLRHost.h"
#include "CCLRHostException.h"
#include "Log.h"
#include "Utility/StringUtils.h"

namespace Wrapper
{
namespace CLR
{
static const std::wstring_view coreCLRInstallDirectory{ L"%programfiles%\\dotnet\\shared\\Microsoft.NETCore.App\\" };
static const std::wstring_view CoreCLRDll{ L"coreclr.dll" };

CCLRHost::CCLRHost( const std::wstring& targetAppPath, const std::vector<std::string>& supportedDotNetCoreVersions )
{
	std::wstring coreRoot;
	m_CoreCLR = { LoadCoreCLRModule( targetAppPath, supportedDotNetCoreVersions, coreRoot ) };

	m_pRuntimeHost = GetHostInterface();

	StartRuntime();

	m_DomainID = CreateAppDomain( targetAppPath, coreRoot );
}

CCLRHost::~CCLRHost()
{
	if( nullptr != m_pRuntimeHost )
	{
		m_pRuntimeHost->UnloadAppDomain( m_DomainID, true /* Wait until unload complete */ );
		m_pRuntimeHost->Stop();
		m_pRuntimeHost->Release();
	}
}

void* CCLRHost::LoadAssemblyAndGetEntryPoint( const std::wstring& assemblyName, const std::wstring& entryPointClass, const std::wstring& entryPointMethod )
{
	void* pfnDelegate = nullptr;

	auto hr = m_pRuntimeHost->CreateDelegate(
		m_DomainID,
		assemblyName.c_str(),		// Target managed assembly
		entryPointClass.c_str(),	// Target managed type
		entryPointMethod.c_str(),	// Target entry point (static method)
		reinterpret_cast<INT_PTR*>( &pfnDelegate ) );

	if( FAILED( hr ) )
	{
		throw CCLRHostException( "Failed to create delegate", hr );
	}

	return pfnDelegate;
}

Utility::CLibrary CCLRHost::LoadCoreCLR( const std::wstring& directoryPath )
{
	auto coreDllPath{ directoryPath };

	coreDllPath += L"\\";
	coreDllPath += CoreCLRDll;

	auto ret = Utility::CLibrary( coreDllPath );

	return ret;
}

Utility::CLibrary CCLRHost::LoadCoreCLRModule( const std::wstring_view& targetAppPath, const std::vector<std::string>& supportedDotNetCoreVersions, std::wstring& coreRoot )
{
	// Look in %CORE_ROOT%
	coreRoot = Utility::GetEnvVariable( "CORE_ROOT" );

	auto coreCLRModule{ LoadCoreCLR( coreRoot ) };

	// If CoreCLR.dll wasn't in %CORE_ROOT%, look next to the target app
	if( !coreCLRModule )
	{
		coreRoot = targetAppPath;
		coreCLRModule = LoadCoreCLR( coreRoot );
	}

	// If CoreCLR.dll wasn't in %CORE_ROOT% or next to the app, 
	// look in the common 1.1.0 install directory
	if( !coreCLRModule )
	{
		for( const auto& version : supportedDotNetCoreVersions )
		{
			std::wstring fullInstallPath{ coreCLRInstallDirectory };
			fullInstallPath += Utility::ToWideString( version );

			coreRoot = Utility::ExpandEnvironmentVariables( fullInstallPath );
			coreCLRModule = LoadCoreCLR( coreRoot );

			if( coreCLRModule )
			{
				break;
			}
		}
	}

	if( !coreCLRModule )
	{
		throw CCLRHostException( "CoreCLR.dll could not be found" );
	}

	Log::Message( "CoreCLR loaded from %ls", coreRoot.c_str() );

	return coreCLRModule;
}

ICLRRuntimeHost2* CCLRHost::GetHostInterface()
{
	auto pfnGetCLRRuntimeHost = m_CoreCLR.GetAddress<FnGetCLRRuntimeHost>( "GetCLRRuntimeHost" );

	if( !pfnGetCLRRuntimeHost )
	{
		throw CCLRHostException( "GetCLRRuntimeHost not found" );
	}

	ICLRRuntimeHost2* pRuntimeHost;

	// Get the hosting interface
	auto hr = pfnGetCLRRuntimeHost( IID_ICLRRuntimeHost2, ( IUnknown** ) &pRuntimeHost );

	if( FAILED( hr ) )
	{
		throw CCLRHostException( "Failed to get ICLRRuntimeHost2 instance", hr );
	}

	return pRuntimeHost;
}

void CCLRHost::StartRuntime()
{
	auto hr = m_pRuntimeHost->SetStartupFlags(
		// These startup flags control runtime-wide behaviors.
		// A complete list of STARTUP_FLAGS can be found in mscoree.h,
		// but some of the more common ones are listed below.
		static_cast<STARTUP_FLAGS>(
			// STARTUP_FLAGS::STARTUP_SERVER_GC |								// Use server GC
			// STARTUP_FLAGS::STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN |		// Maximize domain-neutral loading
			// STARTUP_FLAGS::STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN_HOST |	// Domain-neutral loading for strongly-named assemblies
			STARTUP_FLAGS::STARTUP_CONCURRENT_GC |						// Use concurrent GC
			STARTUP_FLAGS::STARTUP_SINGLE_APPDOMAIN |					// All code executes in the default AppDomain 
																		// (required to use the runtimeHost->ExecuteAssembly helper function)
			STARTUP_FLAGS::STARTUP_LOADER_OPTIMIZATION_SINGLE_DOMAIN	// Prevents domain-neutral loading
			)
	);

	if( FAILED( hr ) )
	{
		throw CCLRHostException( "Failed to set startup flags", hr );
	}

	// Starting the runtime will initialize the JIT, GC, loader, etc.
	hr = m_pRuntimeHost->Start();
	if( FAILED( hr ) )
	{
		throw CCLRHostException( "Failed to start the runtime", hr );
	}

	Log::Message( "Runtime started" );
}

DWORD CCLRHost::CreateAppDomain( const std::wstring& targetAppPath, const std::wstring& coreRoot )
{
	int appDomainFlags =
		// APPDOMAIN_FORCE_TRIVIAL_WAIT_OPERATIONS |		// Do not pump messages during wait
		// APPDOMAIN_SECURITY_SANDBOXED |					// Causes assemblies not from the TPA list to be loaded as partially trusted
		APPDOMAIN_ENABLE_PLATFORM_SPECIFIC_APPS |			// Enable platform-specific assemblies to run
		APPDOMAIN_ENABLE_PINVOKE_AND_CLASSIC_COMINTEROP |	// Allow PInvoking from non-TPA assemblies
		APPDOMAIN_DISABLE_TRANSPARENCY_ENFORCEMENT;			// Entirely disables transparency checks 
															// </Snippet5>

															// <Snippet6>
															// TRUSTED_PLATFORM_ASSEMBLIES
															// "Trusted Platform Assemblies" are prioritized by the loader and always loaded with full trust.
															// A common pattern is to include any assemblies next to CoreCLR.dll as platform assemblies.
															// More sophisticated hosts may also include their own Framework extensions (such as AppDomain managers)
															// in this list.

	std::wstring trustedPlatformAssemblies;

	// Extensions to probe for when finding TPA list files
	const std::vector<std::wstring> tpaExtensions =
	{
		L"*.dll",
		L"*.exe",
		L"*.winmd"
	};

	// Probe next to CoreCLR.dll for any files matching the extensions from tpaExtensions and
	// add them to the TPA list. In a real host, this would likely be extracted into a separate function
	// and perhaps also run on other directories of interest.
	for( const auto& extension : tpaExtensions )
	{
		// Construct the file name search pattern
		auto searchPath{ coreRoot + L"\\" + extension };

		// Find files matching the search pattern
		WIN32_FIND_DATAW findData;
		HANDLE fileHandle = FindFirstFileW( searchPath.c_str(), &findData );

		if( fileHandle != INVALID_HANDLE_VALUE )
		{
			do
			{
				// Construct the full path of the trusted assembly
				auto pathToAdd{ coreRoot + L"\\" + findData.cFileName };

				// Add the assembly to the list and delimited with a semi-colon
				trustedPlatformAssemblies += pathToAdd + L';';

				// Note that the CLR does not guarantee which assembly will be loaded if an assembly
				// is in the TPA list multiple times (perhaps from different paths or perhaps with different NI/NI.dll
				// extensions. Therefore, a real host should probably add items to the list in priority order and only
				// add a file if it's not already present on the list.
				//
				// For this simple sample, though, and because we're only loading TPA assemblies from a single path,
				// we can ignore that complication.
			}
			while( FindNextFileW( fileHandle, &findData ) );
			FindClose( fileHandle );
		}
	}


	// APP_PATHS
	// App paths are directories to probe in for assemblies which are not one of the well-known Framework assemblies
	// included in the TPA list.
	//
	// For this simple sample, we just include the directory the target application is in.
	// More complex hosts may want to also check the current working directory or other
	// locations known to contain application assets.
	// Just use the targetApp provided by the user and remove the file name
	std::wstring appPaths{ targetAppPath };

	// APP_NI_PATHS
	// App (NI) paths are the paths that will be probed for native images not found on the TPA list. 
	// It will typically be similar to the app paths.
	// For this sample, we probe next to the app and in a hypothetical directory of the same name with 'NI' suffixed to the end.
	std::wstring appNiPaths{ targetAppPath };

	appNiPaths += L";";
	appNiPaths += targetAppPath;
	appNiPaths += L"NI";

	// NATIVE_DLL_SEARCH_DIRECTORIES
	// Native dll search directories are paths that the runtime will probe for native DLLs called via PInvoke
	std::wstring nativeDllSearchDirectories{ appPaths };
	nativeDllSearchDirectories += L";";
	nativeDllSearchDirectories += coreRoot;

	// PLATFORM_RESOURCE_ROOTS
	// Platform resource roots are paths to probe in for resource assemblies (in culture-specific sub-directories)
	std::wstring platformResourceRoots{ appPaths };

	// AppDomainCompatSwitch
	// Specifies compatibility behavior for the app domain. This indicates which compatibility
	// quirks to apply if an assembly doesn't have an explicit Target Framework Moniker. If a TFM is
	// present on an assembly, the runtime will always attempt to use quirks appropriate to the version
	// of the TFM.
	// 
	// Typically the latest behavior is desired, but some hosts may want to default to older Silverlight
	// or Windows Phone behaviors for compatibility reasons.
	const std::wstring appDomainCompatSwitch{ L"UseLatestBehaviorWhenTFMNotSpecified" };

	// Setup key/value pairs for AppDomain  properties
	const wchar_t* propertyKeys[] =
	{
		L"TRUSTED_PLATFORM_ASSEMBLIES",
		L"APP_PATHS",
		L"APP_NI_PATHS",
		L"NATIVE_DLL_SEARCH_DIRECTORIES",
		L"PLATFORM_RESOURCE_ROOTS",
		L"AppDomainCompatSwitch"
	};

	// Property values which were constructed in step 5
	const wchar_t* propertyValues[] =
	{
		trustedPlatformAssemblies.c_str(),
		appPaths.c_str(),
		appNiPaths.c_str(),
		nativeDllSearchDirectories.c_str(),
		platformResourceRoots.c_str(),
		appDomainCompatSwitch.c_str()
	};

	DWORD domainId;

	// Create the AppDomain
	auto hr = m_pRuntimeHost->CreateAppDomainWithManager(
		L"SharpLife GoldSource wrapper",	// Friendly AD name
		appDomainFlags,
		nullptr,							// Optional AppDomain manager assembly name
		nullptr,							// Optional AppDomain manager type (including namespace)
		sizeof( propertyKeys ) / sizeof( wchar_t* ),
		propertyKeys,
		propertyValues,
		&domainId );

	if( FAILED( hr ) )
	{
		throw CCLRHostException( "Failed to create AppDomain", hr );
	}

	Log::Message( "AppDomain %d created", domainId );

	return domainId;
}
}
}
