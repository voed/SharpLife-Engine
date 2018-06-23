#ifndef WRAPPER_CLR_CCLRHOSTEXCEPTION_H
#define WRAPPER_CLR_CCLRHOSTEXCEPTION_H

#include <stdexcept>
#include "Common/winsani_in.h"
#include <mscoree.h>
#include "Common/winsani_out.h"

namespace Wrapper
{
namespace CLR
{
class CCLRHostException : public std::domain_error
{
public:
	CCLRHostException( const char* const message, HRESULT errorCode = NOERROR )
		: std::domain_error( message )
		, m_HResult( errorCode )
	{
	}

	bool HasResultCode() const { return FAILED( m_HResult ); }

	HRESULT GetResultCode() const { return m_HResult; }

private:
	HRESULT m_HResult = NOERROR;
};
}
}

#endif //WRAPPER_CLR_CCLRHOSTEXCEPTION_H
