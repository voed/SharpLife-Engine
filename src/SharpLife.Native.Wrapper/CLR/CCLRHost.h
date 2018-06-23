#ifndef WRAPPER_CLR_CCLRHOST_H
#define WRAPPER_CLR_CCLRHOST_H

#include <string>
#include <vector>

#include "Common/winsani_in.h"
#include <mscoree.h>
#include "Common/winsani_out.h"

#include "Utility/CLibrary.h"

namespace Wrapper
{
namespace CLR
{
/**
*	@brief manages a CLR host instance
*/
class CCLRHost final
{
public:
	CCLRHost( const std::wstring& targetAppPath, const std::vector<std::string>& supportedDotNetCoreVersions );
	~CCLRHost();

	void* LoadAssemblyAndGetEntryPoint( const std::wstring& assemblyName, const std::wstring& entryPointClass, const std::wstring& entryPointMethod );

private:
	static Utility::CLibrary LoadCoreCLR( const std::wstring& directoryPath );

	static Utility::CLibrary LoadCoreCLRModule( const std::wstring_view& targetAppPath, const std::vector<std::string>& supportedDotNetCoreVersions, std::wstring& coreRoot );

	ICLRRuntimeHost2* GetHostInterface();

	void StartRuntime();

	DWORD CreateAppDomain( const std::wstring& targetAppPath, const std::wstring& coreRoot );

private:
	Utility::CLibrary m_CoreCLR;

	ICLRRuntimeHost2* m_pRuntimeHost = nullptr;

	DWORD m_DomainID = 0;

private:
	CCLRHost( const CCLRHost& ) = delete;
	CCLRHost& operator=( const CCLRHost& ) = delete;
	CCLRHost( CCLRHost&& ) = delete;
	CCLRHost& operator=( CCLRHost&& ) = delete;
};
}
}

#endif //WRAPPER_CLR_CCLRHOST_H
