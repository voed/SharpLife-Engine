#include "SDL2/SDL.h"

#include "CManagedHost.h"

#include "Dlls/extdll.h"
#include "Engine/APIProxy.h"

/**
*	@file
*
*	This file contains all dllexports used by the engine to load mods
*	
*	SharpLife works by hijacking the game process, this is done by simply not returning execution to the engine
*	In this manner our mods will still adhere to the SDK license (mod running under the Half-Life engine), while giving us complete control over engine level code
*	
*	This also gives us access to Half-Life's SteamWorks permissions, since it's process based
*	This lets us access the authentication system and set up server listings without needing a new App Id
*	
*	To minimize stack size our wrapper state is made global, this reduces the amount of space required on the stack, ensuring that the CLR has as much as possible to run with
*/

//Export the function as an ordinal so the engine can get at it
//Required because the function is a stdcall function, and can't be dllexported directly
#if defined( _WIN32 ) && !defined( __GNUC__ ) && defined ( _MSC_VER )
#pragma comment( linker, "/EXPORT:GiveFnptrsToDll=_GiveFnptrsToDll@8,@1" )
#pragma comment( linker, "/SECTION:.data,RW" )
#endif

#ifdef WIN32
#define GIVEFNPTRSTODLL_DLLEXPORT __stdcall
#else
#define GIVEFNPTRSTODLL_DLLEXPORT WRAPPER_DLLEXPORT
#endif

extern "C"
{
//To minimize stack size we'll make this a global, and set the input arguments as members
Wrapper::CManagedHost g_Host;

//The bare minimum required for clients to load is to provide the exported F function, and then hijacking in Initialize
//The bare minimum required for servers to load is to provide the exported GiveFnptrsToDll function, and hijacking in it
//We need to know the game directory in order to load the correct SharpLife libraries, so we need to get to Initialize for clients

int WRAPPER_DLLEXPORT Initialize( cl_enginefunc_t* pEnginefuncs, int iVersion );

void WRAPPER_DLLEXPORT F( cldll_func_t* pcldll_func )
{
	// Hack!
	//Don't need this
	//g_pcldstAddrs = ( ( cldll_func_dst_t * ) pcldll_func->pHudVidInitFunc );

	pcldll_func->pInitFunc = &Initialize;
}

int Initialize( cl_enginefunc_t* pEnginefuncs, int iVersion )
{
	if( iVersion != CLDLL_INTERFACE_VERSION )
	{
		return false;
	}

	//Find and destroy the engine window
	//This is a dirty hack, but it eliminates problems with the other window being visible and interfering with the other SDL library
	for( Uint32 i = 0; i < 10; ++i )
	{
		auto pWindow = SDL_GetWindowFromID( i );

		if( pWindow )
		{
			SDL_DestroyWindow( pWindow );
		}
	}

	g_Host.Initialize( pEnginefuncs->pfnGetGameDirectory(), false );
	g_Host.Start();
}

void GIVEFNPTRSTODLL_DLLEXPORT GiveFnptrsToDll( enginefuncs_t* pengfuncsFromEngine, globalvars_t* pGlobals )
{
	//Wrapping this in its own scope allows the array to be popped off the stack
	//We don't store the buffer in the host, so we can get away with this
	{
		char szGameDir[ MAX_PATH ];

		pengfuncsFromEngine->pfnGetGameDir( szGameDir );

		g_Host.Initialize( szGameDir, true );
	}

	g_Host.Start();
}
}