#ifndef WRAPPER_LOG_H
#define WRAPPER_LOG_H

#include <string>

namespace Wrapper
{
namespace Log
{
void Message( const char* pszFormat, ... );

void SetDebugLoggingEnabled( bool bEnable );
}
}

#endif //WRAPPER_LOG_H
