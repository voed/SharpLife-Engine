#include "CLibrary.h"

#ifdef WIN32
#include <Windows.h>
#else
#error "Not implemented"
#endif

namespace Wrapper
{
namespace Utility
{
CLibrary::CLibrary()
{
}

CLibrary::CLibrary( const std::wstring& libraryName )
{
#ifdef WIN32
	m_pHandle = LoadLibraryExW( libraryName.c_str(), nullptr, 0 );
#else

#endif
}

CLibrary::CLibrary( CLibrary&& other )
	: m_pHandle( other.m_pHandle )
{
	other.m_pHandle = nullptr;
}

CLibrary& CLibrary::operator=( CLibrary&& other )
{
	if( this != &other )
	{
		m_pHandle = other.m_pHandle;
		other.m_pHandle = nullptr;
	}

	return *this;
}

CLibrary::~CLibrary()
{
#ifdef WIN32
	if( nullptr != m_pHandle )
	{
		FreeLibrary( reinterpret_cast<HMODULE>( m_pHandle ) );
	}
#else

#endif
}

void* CLibrary::GetAddress( const std::string& functionName )
{
	if( nullptr == m_pHandle )
	{
		return nullptr;
	}

	std::string name{ functionName };

#ifdef WIN32
	return ::GetProcAddress( reinterpret_cast<HMODULE>( m_pHandle ), name.c_str() );
#endif
}
}
}
