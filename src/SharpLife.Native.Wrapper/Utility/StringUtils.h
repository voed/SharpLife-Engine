#ifndef WRAPPER_UTILITY_STRINGUTILS_H
#define WRAPPER_UTILITY_STRINGUTILS_H

#include <string>

namespace Wrapper
{
namespace Utility
{
std::string ToNarrowString( const std::wstring& str );

std::wstring ToWideString( const std::string& str );

std::wstring GetEnvVariable( const std::string& name );

std::wstring GetEnvVariable( const std::wstring& name );

std::wstring ExpandEnvironmentVariables( const std::wstring_view& str );

std::wstring GetAbsolutePath( const std::wstring& relativePath );
}
}

#endif //WRAPPER_UTILITY_STRINGUTILS_H
