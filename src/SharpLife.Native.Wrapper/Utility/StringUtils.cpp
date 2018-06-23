#include <cstdio>
#include <memory>
#include <wchar.h>

#include "StringUtils.h"

#ifdef WIN32
#include <Windows.h>
#endif

namespace Wrapper
{
namespace Utility
{
std::string ToNarrowString( const std::wstring& str )
{
	const auto size{ str.size() + 1 };

	auto buf = std::make_unique<char[]>( size );

	snprintf( buf.get(), size, "%ls", str.c_str() );

	std::string rval{ buf.get() };

	return rval;
}

std::wstring ToWideString( const std::string& str )
{
	const auto size{ ( str.size() + 1 ) * sizeof( wchar_t ) };

	auto buf = std::make_unique<wchar_t[]>( size );

	swprintf( buf.get(), size, L"%S", str.c_str() );

	std::wstring rval{ buf.get() };

	return rval;
}

std::wstring GetEnvVariable( const std::string& name )
{
	if( auto pValue = std::getenv( name.c_str() ) )
	{
		return ToWideString( pValue );
	}

	return {};
}

std::wstring GetEnvVariable( const std::wstring& name )
{
	if( auto pValue = std::getenv( ToNarrowString( name ).c_str() ) )
	{
		return ToWideString( pValue );
	}

	return {};
}

static bool ExpandEnvironmentVariable( const std::wstring_view prefix, const std::wstring_view suffix, const std::wstring& str, std::wstring& result )
{
	auto index = str.find( prefix );

	if( std::wstring::npos == index )
	{
		return false;
	}

	auto end = str.find( suffix, index + prefix.length() );

	if( std::wstring::npos == end )
	{
		return false;
	}

	const auto variable{ str.substr( index + prefix.length(), end - ( index + prefix.length() ) ) };

	auto value = GetEnvVariable( variable );

	auto pre = str.substr( 0, index );
	auto post = str.substr( end + suffix.length() );

	result = pre + value + post;
	return true;
}

std::wstring ExpandEnvironmentVariables( const std::wstring_view& str )
{
	std::wstring result{ str };

	while(
		ExpandEnvironmentVariable( L"${", L"}", result, result ) ||
		ExpandEnvironmentVariable( L"%", L"%", result, result ) )
	{
	}

	return result;
}

std::wstring GetAbsolutePath( const std::wstring& relativePath )
{
#ifdef WIN32
	const auto size = GetFullPathNameW( relativePath.c_str(), 0, nullptr, nullptr );

	auto buf = std::make_unique<wchar_t[]>( size );

	GetFullPathNameW( relativePath.c_str(), size, buf.get(), nullptr );

	return buf.get();
#else
#error "Implement Me"
#endif
}
}
}
