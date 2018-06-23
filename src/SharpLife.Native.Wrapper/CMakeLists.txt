cmake_minimum_required( VERSION 3.11 )

set( STEAMCOMMON "$ENV{STEAMCOMMON}" CACHE PATH "Steam common directory path" )

set( GAME_DIRECTORY "Half-Life" CACHE STRING "Game directory name" )
set( MOD_DIRECTORY "sharplife" CACHE STRING "Mod directory name" )

set( GAME_BASE_PATH ${STEAMCOMMON}/${GAME_DIRECTORY}/${MOD_DIRECTORY} )

set( NATIVE_DLLS_DIRECTORY "dlls" )
set( NATIVE_CL_DLLS_DIRECTORY "cl_dlls" )

project( Native-Wrapper LANGUAGES C CXX )

set( CMAKE_CXX_STANDARD 17 )
set( CMAKE_CXX_STANDARD_REQUIRED ON )

#
#	Important note about native library usage in SharpLife:
#	In order to take over execution and load our managed code, our native wrappers are loaded by the engine,
#	which then start the CLR host and continues execution in managed code
#	
#	Both the client and server mod libraries are the same library, client.ext
#	The engine will always load the client from <moddir>/cl_dlls/client.ext, and since the server differs only very slightly in startup it's handled in the same library
#	Both the client and server will thus be launched from client.ext
#	
#	This makes for a very simple way to handle things because very little native code is required to make this work
#	Note that the engine will only ever load the client (hl.exe) or server (hlds.exe), once either is loaded the original engine no longer does anything
#	Our engine will exit by returning to mod code, which then calls std::quick_exit,
#	which avoids running the engine's original shutdown code, which would otherwise print warnings in output
#

set( TARGET_NAME client )

add_library( ${TARGET_NAME} SHARED )

target_sources( ${TARGET_NAME} PRIVATE
	CLR/CCLRHost.cpp
	CLR/CCLRHost.h
	CLR/CCLRHostException.h
	Common/const.h
	Common/crc.h
	Common/cvardef.h
	Common/entity_state.h
	Common/enums.h
	Common/in_buttons.h
	Common/netadr.h
	Common/pmtrace.h
	Common/Sequence.h
	Common/usercmd.h
	Common/weaponinfo.h
	Common/winsani_in.h
	Common/winsani_out.h
	Common/wrect.h
	Dlls/cdll_dll.h
	Dlls/enginecallback.h
	Dlls/extdll.h
	Dlls/vector.h
	Engine/custom.h
	Engine/edict.h
	Engine/eiface.h
	Engine/progdefs.h
	Engine/progs.h
	Public/Steam/steamtypes.h
	Public/archtypes.h
	Utility/CLibrary.cpp
	Utility/CLibrary.h
	Utility/StringUtils.cpp
	Utility/StringUtils.h
	CConfiguration.h
	CManagedHost.cpp
	CManagedHost.h
	ConfigurationInput.cpp
	ConfigurationInput.h
	EngineInterface.cpp
	Log.cpp
	Log.h
)

get_target_property( sources ${TARGET_NAME} SOURCES )

source_group( TREE ${CMAKE_CURRENT_LIST_DIR} PREFIX ${TARGET_NAME} FILES ${sources} )

target_include_directories( ${TARGET_NAME} PRIVATE
	${CMAKE_CURRENT_LIST_DIR}
	${CMAKE_CURRENT_LIST_DIR}/../../external/inih/include
	${CMAKE_CURRENT_LIST_DIR}/../../external/dotnetcore/include
)

target_compile_definitions( ${TARGET_NAME} PRIVATE
	$<$<CXX_COMPILER_ID:MSVC>:_CRT_SECURE_NO_WARNINGS>
)

set_target_properties( ${TARGET_NAME} PROPERTIES
	LIBRARY_OUTPUT_DIRECTORY "${GAME_BASE_PATH}/${NATIVE_CL_DLLS_DIRECTORY}"
	RUNTIME_OUTPUT_DIRECTORY_DEBUG "${GAME_BASE_PATH}/${NATIVE_CL_DLLS_DIRECTORY}"
	RUNTIME_OUTPUT_DIRECTORY_RELEASE "${GAME_BASE_PATH}/${NATIVE_CL_DLLS_DIRECTORY}"
)

set_property( DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR} PROPERTY VS_STARTUP_PROJECT ${TARGET_NAME} )
