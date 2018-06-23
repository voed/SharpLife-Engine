#ifndef WRAPPER_CBASEMANAGEDHOST_H
#define WRAPPER_CBASEMANAGEDHOST_H

#include <memory>
#include <vector>

#include "CConfiguration.h"
#include "CLR/CCLRHost.h"
#include "Utility/CLibrary.h"

namespace Wrapper
{
/**
*	@brief Base class for SharpLife managed hosts
*/
class CManagedHost
{
private:
	static const std::string_view CONFIG_FILENAME;

public:
	CManagedHost();
	~CManagedHost();

public:
	void Initialize( std::string&& szGameDir, bool bIsServer );

	[[noreturn]] void Start();

private:
	CConfiguration& GetConfiguration() { return m_Configuration; }

	CLR::CCLRHost& GetCLRHost() { return *m_CLRHost; }

	bool LoadConfiguration();

	bool StartManagedHost();

	void ShutdownManagedHost();

private:
	CConfiguration m_Configuration;

	//The host for the managed code runtime
	std::unique_ptr<CLR::CCLRHost> m_CLRHost;

	std::string m_szGameDir;
	bool m_bIsServer = false;

private:
	CManagedHost( const CManagedHost& ) = delete;
	CManagedHost& operator=( const CManagedHost& ) = delete;
};
}

#endif //WRAPPER_CBASEMANAGEDHOST_H
