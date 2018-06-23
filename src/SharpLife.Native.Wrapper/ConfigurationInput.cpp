#include <cstdint>
#include <cstdio>
#include <inih/INIReader.h>

#ifdef WIN32
#include <Windows.h>
#endif

#include "ConfigurationInput.h"
#include "Log.h"

namespace Wrapper
{
static void GetEntryPoint( const std::string& szSectionName, INIReader& reader, CConfiguration::CManagedEntryPoint& entryPoint )
{
	entryPoint.Path = reader.Get( szSectionName, "Path", "" );
	entryPoint.AssemblyName = reader.Get( szSectionName, "AssemblyName", "" );
	entryPoint.Class = reader.Get( szSectionName, "Class", "" );
	entryPoint.Method = reader.Get( szSectionName, "Method", "" );
}

std::optional<CConfiguration> LoadConfiguration( const std::string& szFileName )
{
	INIReader reader( szFileName );

	if( reader.ParseError() < 0 )
	{
		Log::Message( "Error parsing INI file: %d", reader.ParseError() );
		return {};
	}

	CConfiguration config;

	config.DebugLoggingEnabled = reader.GetBoolean( "SharpLife", "DebugLoggingEnabled", false );

	const auto numDotNetCoreVersions = reader.GetInteger( "DotNetCoreVersions", "Count", 0 );

	config.SupportedDotNetCoreVersions.reserve( numDotNetCoreVersions );

	for( long i = 0; i < numDotNetCoreVersions; ++i )
	{
		auto version = reader.Get( "DotNetCoreVersions", std::to_string( i ) + "/Version", "" );

		if( !version.empty() )
		{
			config.SupportedDotNetCoreVersions.emplace_back( std::move( version ) );
		}
	}

	GetEntryPoint( "Managed", reader, config.ManagedEntryPoint );

	return config;
}
}
