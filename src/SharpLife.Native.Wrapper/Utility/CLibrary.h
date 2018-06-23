#ifndef WRAPPER_UTILITY_CLIBRARY_H
#define WRAPPER_UTILITY_CLIBRARY_H

#include <string>

namespace Wrapper
{
namespace Utility
{
class CLibrary final
{
public:
	CLibrary();
	CLibrary( const std::wstring& libraryName );
	CLibrary( CLibrary&& );
	CLibrary& operator=( CLibrary&& );
	~CLibrary();

	operator bool() const { return nullptr != m_pHandle; }

	void* GetAddress( const std::string& functionName );

	template<typename FUNCTION>
	FUNCTION GetAddress( const std::string& functionName )
	{
		return reinterpret_cast<FUNCTION>( GetAddress( functionName ) );
	}

private:
	void* m_pHandle = nullptr;

private:
	CLibrary( const CLibrary& ) = delete;
	CLibrary& operator=( const CLibrary& ) = delete;
};
}
}

#endif //WRAPPER_UTILITY_CLIBRARY_H
