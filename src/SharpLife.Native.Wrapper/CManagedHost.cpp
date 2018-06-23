#include "CManagedHost.h"
#include "CLR/CCLRHostException.h"
#include "ConfigurationInput.h"
#include "Log.h"
#include "Utility/StringUtils.h"

namespace Wrapper
{
const std::string_view CManagedHost::CONFIG_FILENAME{ "cfg/SharpLife-Wrapper-Native.ini" };

using ManagedEntryPoint = int ( STDMETHODCALLTYPE* )( bool bIsServer );

CManagedHost::CManagedHost() = default;

CManagedHost::~CManagedHost() = default;

void CManagedHost::Initialize( std::string&& szGameDir, bool bIsServer )
{
	m_szGameDir = std::move( szGameDir );
	m_bIsServer = bIsServer;
}

void CManagedHost::Start()
{
	int exitCode = 1;

	if( LoadConfiguration() )
	{
		if( StartManagedHost() )
		{
			try
			{
				auto entryPoint = reinterpret_cast< ManagedEntryPoint >( m_CLRHost->LoadAssemblyAndGetEntryPoint(
					Utility::ToWideString( m_Configuration.ManagedEntryPoint.AssemblyName ),
					Utility::ToWideString( m_Configuration.ManagedEntryPoint.Class ),
					Utility::ToWideString( m_Configuration.ManagedEntryPoint.Method )
				) );

				exitCode = entryPoint( m_bIsServer );
			}
			catch( const CLR::CCLRHostException& e )
			{
				if( e.HasResultCode() )
				{
					Log::Message( "ERROR - %s\nError code:%x", e.what(), e.GetResultCode() );
				}
				else
				{
					Log::Message( "ERROR - %s", e.what() );
				}
			}

			ShutdownManagedHost();
		}
	}

	std::quick_exit( exitCode );
}

bool CManagedHost::LoadConfiguration()
{
	auto config = Wrapper::LoadConfiguration( m_szGameDir + '/' + std::string{ CONFIG_FILENAME } );

	if( config )
	{
		m_Configuration = std::move( config.value() );
		Log::SetDebugLoggingEnabled( m_Configuration.DebugLoggingEnabled );
		return true;
	}

	return false;
}

bool CManagedHost::StartManagedHost()
{
	auto dllsPath = Utility::ToWideString( m_szGameDir ) + L'/' + Utility::ToWideString( m_Configuration.ManagedEntryPoint.Path );

	dllsPath = Utility::GetAbsolutePath( dllsPath );

	try
	{
		m_CLRHost = std::make_unique<CLR::CCLRHost>( dllsPath, m_Configuration.SupportedDotNetCoreVersions );
	}
	catch( const CLR::CCLRHostException e )
	{
		if( e.HasResultCode() )
		{
			Log::Message( "ERROR - %s\nError code:%x", e.what(), e.GetResultCode() );
		}
		else
		{
			Log::Message( "ERROR - %s", e.what() );
		}

		return false;
	}

	return true;
}

void CManagedHost::ShutdownManagedHost()
{
	m_CLRHost.release();
}
}
