#ifndef WRAPPER_CCONFIGURATION_H
#define WRAPPER_CCONFIGURATION_H

#include <string>
#include <unordered_map>
#include <vector>

namespace Wrapper
{
/**
*	@brief Stores the configuration for the wrapper
*/
class CConfiguration final
{
public:
	struct CManagedEntryPoint final
	{
		std::string Path;
		std::string AssemblyName;
		std::string Class;
		std::string Method;
	};

public:
	CConfiguration() = default;
	~CConfiguration() = default;
	CConfiguration( CConfiguration&& ) = default;
	CConfiguration& operator=( CConfiguration&& ) = default;

	/**
	*	@brief Whether debug logging is enabled
	*/
	bool DebugLoggingEnabled = false;

	/**
	*	@brief List of supported dot net core versions
	*	Ordered from most to least important (usually newest to oldest), used to find the runtime install directory
	*/
	std::vector<std::string> SupportedDotNetCoreVersions;

	CManagedEntryPoint ManagedEntryPoint;

private:
	CConfiguration( const CConfiguration& ) = delete;
	CConfiguration& operator=( const CConfiguration& ) = delete;
};
}

#endif //WRAPPER_CCONFIGURATION_H
