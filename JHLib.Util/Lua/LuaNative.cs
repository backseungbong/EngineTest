using System.Runtime.InteropServices;
using System.Text;

using lua_Alloc = System.IntPtr;
using lua_CFunction = System.IntPtr;
using lua_Debug = System.IntPtr;
using lua_Hook = System.IntPtr;
using lua_Integer = System.Int64;
using lua_KContext = System.IntPtr;
using lua_KFunction = System.IntPtr;
using lua_Number = System.Double;
using lua_Reader = System.IntPtr;
using lua_State = System.IntPtr;
using lua_WarnFunction = System.IntPtr;
using lua_Writer = System.IntPtr;
using pCHAR = System.IntPtr;
using pSIZE = System.UIntPtr;
using pVOID = System.IntPtr;

namespace JHLib.Util.Lua
{
    internal static unsafe class LuaNative
    {
        private const string LIBName = "libluajit";

        // 함수 포인터 선언
        private static readonly delegate* unmanaged[Cdecl]<lua_State> _luaL_newstate;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, void> _luaL_openlibs;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int> _lua_absindex;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, void> _lua_arith;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, lua_CFunction, lua_CFunction> _lua_atpanic;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int, void> _lua_call;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int> _lua_checkstack;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, void> _lua_close;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int, int, int> _lua_compare;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, void> _lua_concat;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int, void> _lua_copy;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int, void> _lua_createtable;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, lua_Writer, pVOID, int, int> _lua_dump;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int> _lua_error;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int, int> _lua_gc;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int, int, int> _lua_gc_ex;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, out pVOID, lua_Alloc> _lua_getallocf;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, pCHAR, int> _lua_getfield;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, lua_Hook> _lua_gethook;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int> _lua_gethookcount;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int> _lua_gethookmask;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, long, int> _lua_geti;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, pCHAR, ref lua_Debug, int> _lua_getinfo;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int, int> _lua_getiuservalue;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, ref lua_Debug, int, pCHAR> _lua_getlocal;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int> _lua_getmetatable;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, ref lua_Debug, int> _lua_getstack;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int> _lua_gettable;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int> _lua_gettop;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int, pCHAR> _lua_getupvalue;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int> _lua_iscfunction;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int> _lua_isinteger;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int> _lua_isnumber;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int> _lua_isstring;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int> _lua_isuserdata;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int> _lua_isyieldable;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, void> _lua_len;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, lua_Reader, pVOID, pCHAR, pCHAR, int> _lua_load;
        private static readonly delegate* unmanaged[Cdecl]<lua_Alloc, pVOID, lua_State> _lua_newstate;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, lua_State> _lua_newthread;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, pSIZE, int, pVOID> _lua_newuserdatauv;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int> _lua_next;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int, int, LuaStatus> _lua_pcall;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, void> _lua_pushboolean;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, lua_CFunction, int, void> _lua_pushcclosure;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, lua_Integer, void> _lua_pushinteger;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, pVOID, void> _lua_pushlightuserdata;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, pCHAR, void> _lua_pushstring;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, pVOID, pSIZE, pCHAR> _lua_pushlstring;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, void> _lua_pushnil;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, lua_Number, void> _lua_pushnumber;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int> _lua_pushthread;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, void> _lua_pushvalue;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int, int> _lua_rawequal;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int> _lua_rawget;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, lua_Integer, int> _lua_rawgeti;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, pVOID, int> _lua_rawgetp;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, pSIZE> _lua_rawlen;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, void> _lua_rawset;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, lua_Integer, void> _lua_rawseti;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, pVOID, void> _lua_rawsetp;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int> _lua_resetthread;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, lua_State, int, out int, int> _lua_resume;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int, void> _lua_rotate;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, lua_Alloc, pVOID, void> _lua_setallocf;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, void> _lua_setfield;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, lua_Hook, int, int, void> _lua_sethook;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, long, void> _lua_seti;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int, void> _lua_setiuservalue;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, ref lua_Debug, int, pCHAR> _lua_setlocal;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, void> _lua_setmetatable;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, void> _lua_settable;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, void> _lua_settop;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int, pCHAR> _lua_setupvalue;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, lua_WarnFunction, pVOID, void> _lua_setwarnf;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int> _lua_status;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, pCHAR, pSIZE> _lua_stringtonumber;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int> _lua_toboolean;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, lua_CFunction> _lua_tocfunction;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, lua_CFunction> _lua_toclose;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, out int, lua_Integer> _lua_tointegerx;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, out pSIZE, pCHAR> _lua_tolstring;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, out int, lua_Number> _lua_tonumberx;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, pVOID> _lua_topointer;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, lua_State> _lua_tothread;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, pVOID> _lua_touserdata;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int> _lua_type;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, pCHAR> _lua_typename;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int, pVOID> _lua_upvalueid;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int, int, int, void> _lua_upvaluejoin;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, lua_Number> _lua_version;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, pCHAR, int, void> _lua_warning;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, lua_State, int, void> _lua_xmove;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, lua_KContext, lua_KFunction, int> _lua_yieldk;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, int> _luaL_argerror;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, int> _luaL_callmeta;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, void> _luaL_checkany;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, lua_Integer> _luaL_checkinteger;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, out pSIZE, pCHAR> _luaL_checklstring;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, lua_Number> _luaL_checknumber;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, pCHAR[], int> _luaL_checkoption;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, void> _luaL_checkstack;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int, void> _luaL_checktype;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, pVOID> _luaL_checkudata;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, lua_Number, pSIZE, void> _luaL_checkversion_;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, pCHAR, int> _luaL_error;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, int> _luaL_execresult;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, int> _luaL_fileresult;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, int> _luaL_getmetafield;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, int> _luaL_getsubtable;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, lua_Integer> _luaL_len;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, byte[], pSIZE, pCHAR, pCHAR, int> _luaL_loadbufferx;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, pCHAR, int> _luaL_loadfile;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, pCHAR, int> _luaL_newmetatable;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, lua_Integer, lua_Integer> _luaL_optinteger;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, lua_Number, lua_Number> _luaL_optnumber;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int> _luaL_ref;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, pCHAR, lua_CFunction, int, void> _luaL_requiref;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, pCHAR, void> _luaL_setmetatable;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, pVOID> _luaL_testudata;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, lua_State, pCHAR, int, pCHAR> _luaL_traceback;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, int> _luaL_typeerror;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int, void> _luaL_unref;
        private static readonly delegate* unmanaged[Cdecl]<lua_State, int, void> _luaL_where;

        private static nint Get(nint handle, string name) => NativeLibrary.GetExport(handle, name);
        static LuaNative()
        {
            Console.WriteLine($"LuaNative initializing... libname:{LIBName}");

            try
            {
                var h = NativeLibrary.Load(LIBName);

                // 함수 포인터 초기화
                _luaL_newstate = (delegate* unmanaged[Cdecl]<lua_State>)Get(h, "luaL_newstate");
                _luaL_openlibs = (delegate* unmanaged[Cdecl]<lua_State, void>)Get(h, "luaL_openlibs");
                //_lua_absindex = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int>)Get(h, "lua_absindex");
                //_lua_arith = (delegate* unmanaged[Cdecl]<lua_State, int, void>)Get(h, "lua_arith");
                _lua_atpanic = (delegate* unmanaged[Cdecl]<lua_State, lua_CFunction, lua_CFunction>)Get(h, "lua_atpanic");
                _lua_call = (delegate* unmanaged[Cdecl]<lua_State, int, int, void>)Get(h, "lua_call");
                _lua_checkstack = (delegate* unmanaged[Cdecl]<lua_State, int, int>)Get(h, "lua_checkstack");
                _lua_close = (delegate* unmanaged[Cdecl]<lua_State, void>)Get(h, "lua_close");
                //_lua_compare = (delegate* unmanaged[Cdecl]<lua_State, int, int, int, int>)Get(h, "lua_compare");
                _lua_concat = (delegate* unmanaged[Cdecl]<lua_State, int, void>)Get(h, "lua_concat");
                _lua_copy = (delegate* unmanaged[Cdecl]<lua_State, int, int, void>)Get(h, "lua_copy");
                _lua_createtable = (delegate* unmanaged[Cdecl]<lua_State, int, int, void>)Get(h, "lua_createtable");
                _lua_dump = (delegate* unmanaged[Cdecl]<lua_State, lua_Writer, pVOID, int, int>)Get(h, "lua_dump");
                _lua_error = (delegate* unmanaged[Cdecl]<lua_State, int>)Get(h, "lua_error");
                _lua_gc = (delegate* unmanaged[Cdecl]<lua_State, int, int, int>)Get(h, "lua_gc");
                _lua_gc_ex = (delegate* unmanaged[Cdecl]<lua_State, int, int, int, int>)Get(h, "lua_gc");
                _lua_getallocf = (delegate* unmanaged[Cdecl]<lua_State, out pVOID, lua_Alloc>)Get(h, "lua_getallocf");
                _lua_getfield = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, pCHAR, int>)Get(h, "lua_getfield");
                _lua_gethook = (delegate* unmanaged[Cdecl]<lua_State, lua_Hook>)Get(h, "lua_gethook");
                _lua_gethookcount = (delegate* unmanaged[Cdecl]<lua_State, int>)Get(h, "lua_gethookcount");
                _lua_gethookmask = (delegate* unmanaged[Cdecl]<lua_State, int>)Get(h, "lua_gethookmask");
                //_lua_geti = (delegate* unmanaged[Cdecl]<lua_State, int, long, int>)Get(h, "lua_geti");
                _lua_getinfo = (delegate* unmanaged[Cdecl]<lua_State, pCHAR, ref lua_Debug, int>)Get(h, "lua_getinfo");
                //_lua_getiuservalue = (delegate* unmanaged[Cdecl]<lua_State, int, int, int>)Get(h, "lua_getiuservalue");
                _lua_getlocal = (delegate* unmanaged[Cdecl]<lua_State, ref lua_Debug, int, pCHAR>)Get(h, "lua_getlocal");
                _lua_getmetatable = (delegate* unmanaged[Cdecl]<lua_State, int, int>)Get(h, "lua_getmetatable");
                _lua_getstack = (delegate* unmanaged[Cdecl]<lua_State, int, ref lua_Debug, int>)Get(h, "lua_getstack");
                _lua_gettable = (delegate* unmanaged[Cdecl]<lua_State, int, int>)Get(h, "lua_gettable");
                _lua_gettop = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int>)Get(h, "lua_gettop");
                _lua_getupvalue = (delegate* unmanaged[Cdecl]<lua_State, int, int, pCHAR>)Get(h, "lua_getupvalue");
                _lua_iscfunction = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int>)Get(h, "lua_iscfunction");
                //_lua_isinteger = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int>)Get(h, "lua_isinteger");
                _lua_isnumber = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int>)Get(h, "lua_isnumber");
                _lua_isstring = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int>)Get(h, "lua_isstring");
                _lua_isuserdata = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int>)Get(h, "lua_isuserdata");
                _lua_isyieldable = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int>)Get(h, "lua_isyieldable");
                //_lua_len = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, void>)Get(h, "lua_len");
                _lua_load = (delegate* unmanaged[Cdecl]<lua_State, lua_Reader, pVOID, pCHAR, pCHAR, int>)Get(h, "lua_load");
                _lua_newstate = (delegate* unmanaged[Cdecl]<lua_Alloc, pVOID, lua_State>)Get(h, "lua_newstate");
                _lua_newthread = (delegate* unmanaged[Cdecl]<lua_State, lua_State>)Get(h, "lua_newthread");
                //_lua_newuserdatauv = (delegate* unmanaged[Cdecl]<lua_State, pSIZE, int, pVOID>)Get(h, "lua_newuserdatauv");
                _lua_next = (delegate* unmanaged[Cdecl]<lua_State, int, int>)Get(h, "lua_next");
                _lua_pcall = (delegate* unmanaged[Cdecl]<lua_State, int, int, int, LuaStatus>)Get(h, "lua_pcall");
                _lua_pushboolean = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, void>)Get(h, "lua_pushboolean");
                _lua_pushcclosure = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, lua_CFunction, int, void>)Get(h, "lua_pushcclosure");
                _lua_pushinteger = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, lua_Integer, void>)Get(h, "lua_pushinteger");
                _lua_pushlightuserdata = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, pVOID, void>)Get(h, "lua_pushlightuserdata");
                _lua_pushstring = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, pCHAR, void>)Get(h, "lua_pushstring");
                _lua_pushlstring = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, pVOID, pSIZE, pCHAR>)Get(h, "lua_pushlstring");
                _lua_pushnil = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, void>)Get(h, "lua_pushnil");
                _lua_pushnumber = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, lua_Number, void>)Get(h, "lua_pushnumber");
                _lua_pushthread = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int>)Get(h, "lua_pushthread");
                _lua_pushvalue = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, void>)Get(h, "lua_pushvalue");
                _lua_rawequal = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int, int>)Get(h, "lua_rawequal");
                _lua_rawget = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int>)Get(h, "lua_rawget");
                _lua_rawgeti = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, lua_Integer, int>)Get(h, "lua_rawgeti");
                //_lua_rawgetp = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, pVOID, int>)Get(h, "lua_rawgetp");
                //_lua_rawlen = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, pSIZE>)Get(h, "lua_rawlen");
                _lua_rawset = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, void>)Get(h, "lua_rawset");
                _lua_rawseti = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, lua_Integer, void>)Get(h, "lua_rawseti");
                //_lua_rawsetp = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, pVOID, void>)Get(h, "lua_rawsetp");
                //_lua_resetthread = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int>)Get(h, "lua_resetthread");
                _lua_resume = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, lua_State, int, out int, int>)Get(h, "lua_resume");
                //_lua_rotate = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int, void>)Get(h, "lua_rotate");
                _lua_setallocf = (delegate* unmanaged[Cdecl]<lua_State, lua_Alloc, pVOID, void>)Get(h, "lua_setallocf");
                _lua_setfield = (delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, void>)Get(h, "lua_setfield");
                _lua_sethook = (delegate* unmanaged[Cdecl]<lua_State, lua_Hook, int, int, void>)Get(h, "lua_sethook");
                //_lua_seti = (delegate* unmanaged[Cdecl]<lua_State, int, long, void>)Get(h, "lua_seti");
                //_lua_setiuservalue = (delegate* unmanaged[Cdecl]<lua_State, int, int, void>)Get(h, "lua_setiuservalue");
                _lua_setlocal = (delegate* unmanaged[Cdecl]<lua_State, ref lua_Debug, int, pCHAR>)Get(h, "lua_setlocal");
                _lua_setmetatable = (delegate* unmanaged[Cdecl]<lua_State, int, void>)Get(h, "lua_setmetatable");
                _lua_settable = (delegate* unmanaged[Cdecl]<lua_State, int, void>)Get(h, "lua_settable");
                _lua_settop = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, void>)Get(h, "lua_settop");
                _lua_setupvalue = (delegate* unmanaged[Cdecl]<lua_State, int, int, pCHAR>)Get(h, "lua_setupvalue");
                //_lua_setwarnf = (delegate* unmanaged[Cdecl]<lua_State, lua_WarnFunction, pVOID, void>)Get(h, "lua_setwarnf");
                _lua_status = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int>)Get(h, "lua_status");
                //_lua_stringtonumber = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, pCHAR, pSIZE>)Get(h, "lua_stringtonumber");
                _lua_toboolean = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int>)Get(h, "lua_toboolean");
                _lua_tocfunction = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, lua_CFunction>)Get(h, "lua_tocfunction");
                //_lua_toclose = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, lua_CFunction>)Get(h, "lua_toclose");
                _lua_tointegerx = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, out int, lua_Integer>)Get(h, "lua_tointegerx");
                _lua_tolstring = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, out pSIZE, pCHAR>)Get(h, "lua_tolstring");
                _lua_tonumberx = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, out int, lua_Number>)Get(h, "lua_tonumberx");
                _lua_topointer = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, pVOID>)Get(h, "lua_topointer");
                _lua_tothread = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, lua_State>)Get(h, "lua_tothread");
                _lua_touserdata = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, pVOID>)Get(h, "lua_touserdata");
                _lua_type = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int>)Get(h, "lua_type");
                _lua_typename = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, pCHAR>)Get(h, "lua_typename");
                _lua_upvalueid = (delegate* unmanaged[Cdecl]<lua_State, int, int, pVOID>)Get(h, "lua_upvalueid");
                _lua_upvaluejoin = (delegate* unmanaged[Cdecl]<lua_State, int, int, int, int, void>)Get(h, "lua_upvaluejoin");
                _lua_version = (delegate* unmanaged[Cdecl]<lua_State, lua_Number>)Get(h, "lua_version");
                //_lua_warning = (delegate* unmanaged[Cdecl]<lua_State, pCHAR, int, void>)Get(h, "lua_warning");
                _lua_xmove = (delegate* unmanaged[Cdecl]<lua_State, lua_State, int, void>)Get(h, "lua_xmove");
                //_lua_yieldk = (delegate* unmanaged[Cdecl]<lua_State, int, lua_KContext, lua_KFunction, int>)Get(h, "lua_yieldk");
                _luaL_argerror = (delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, int>)Get(h, "luaL_argerror");
                _luaL_callmeta = (delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, int>)Get(h, "luaL_callmeta");
                _luaL_checkany = (delegate* unmanaged[Cdecl]<lua_State, int, void>)Get(h, "luaL_checkany");
                _luaL_checkinteger = (delegate* unmanaged[Cdecl]<lua_State, int, lua_Integer>)Get(h, "luaL_checkinteger");
                _luaL_checklstring = (delegate* unmanaged[Cdecl]<lua_State, int, out pSIZE, pCHAR>)Get(h, "luaL_checklstring");
                _luaL_checknumber = (delegate* unmanaged[Cdecl]<lua_State, int, lua_Number>)Get(h, "luaL_checknumber");
                _luaL_checkoption = (delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, pCHAR[], int>)Get(h, "luaL_checkoption");
                _luaL_checkstack = (delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, void>)Get(h, "luaL_checkstack");
                _luaL_checktype = (delegate* unmanaged[Cdecl]<lua_State, int, int, void>)Get(h, "luaL_checktype");
                _luaL_checkudata = (delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, pVOID>)Get(h, "luaL_checkudata");
                //_luaL_checkversion_ = (delegate* unmanaged[Cdecl]<lua_State, lua_Number, pSIZE, void>)Get(h, "luaL_checkversion_");
                _luaL_error = (delegate* unmanaged[Cdecl]<lua_State, pCHAR, int>)Get(h, "luaL_error");
                _luaL_execresult = (delegate* unmanaged[Cdecl]<lua_State, int, int>)Get(h, "luaL_execresult");
                _luaL_fileresult = (delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, int>)Get(h, "luaL_fileresult");
                _luaL_getmetafield = (delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, int>)Get(h, "luaL_getmetafield");
                //_luaL_getsubtable = (delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, int>)Get(h, "luaL_getsubtable");
                //_luaL_len = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, lua_Integer>)Get(h, "luaL_len");
                _luaL_loadbufferx = (delegate* unmanaged[Cdecl]<lua_State, byte[], pSIZE, pCHAR, pCHAR, int>)Get(h, "luaL_loadbufferx");
                _luaL_loadfile = (delegate* unmanaged[Cdecl]<lua_State, pCHAR, int>)Get(h, "luaL_loadfile");
                _luaL_newmetatable = (delegate* unmanaged[Cdecl]<lua_State, pCHAR, int>)Get(h, "luaL_newmetatable");
                _luaL_optinteger = (delegate* unmanaged[Cdecl]<lua_State, int, lua_Integer, lua_Integer>)Get(h, "luaL_optinteger");
                _luaL_optnumber = (delegate* unmanaged[Cdecl]<lua_State, int, lua_Number, lua_Number>)Get(h, "luaL_optnumber");
                _luaL_ref = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int>)Get(h, "luaL_ref");
                //_luaL_requiref = (delegate* unmanaged[Cdecl]<lua_State, pCHAR, lua_CFunction, int, void>)Get(h, "luaL_requiref");
                _luaL_setmetatable = (delegate* unmanaged[Cdecl]<lua_State, pCHAR, void>)Get(h, "luaL_setmetatable");
                _luaL_testudata = (delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, pVOID>)Get(h, "luaL_testudata");
                _luaL_traceback = (delegate* unmanaged[Cdecl]<lua_State, lua_State, pCHAR, int, pCHAR>)Get(h, "luaL_traceback");
                //_luaL_typeerror = (delegate* unmanaged[Cdecl]<lua_State, int, pCHAR, int>)Get(h, "luaL_typeerror");
                _luaL_unref = (delegate* unmanaged[Cdecl, SuppressGCTransition]<lua_State, int, int, void>)Get(h, "luaL_unref");
                _luaL_where = (delegate* unmanaged[Cdecl]<lua_State, int, void>)Get(h, "luaL_where");

                Console.WriteLine($"LuaNative initialized. _luaL_newstate : {(IntPtr)_luaL_newstate}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        internal static int lua_absindex(lua_State hLua, int idx) => _lua_absindex(hLua, idx);
        internal static void lua_arith(lua_State hLua, int op) => _lua_arith(hLua, op);
        internal static lua_CFunction lua_atpanic(lua_State hLua, lua_CFunction panicf) => _lua_atpanic(hLua, panicf);
        internal static void lua_call(lua_State hLua, int nargs, int nresults) => _lua_call(hLua, nargs, nresults);
        internal static int lua_checkstack(lua_State hLua, int extra) => _lua_checkstack(hLua, extra);
        internal static void lua_close(lua_State hLua) => _lua_close(hLua);
        internal static int lua_compare(lua_State hLua, int index1, int index2, int op) => _lua_compare(hLua, index1, index2, op);
        internal static void lua_concat(lua_State hLua, int n) => _lua_concat(hLua, n);
        internal static void lua_copy(lua_State hLua, int fromIndex, int toIndex) => _lua_copy(hLua, fromIndex, toIndex);
        internal static void lua_createtable(lua_State hLua, int elements, int records) => _lua_createtable(hLua, elements, records);
        internal static int lua_dump(lua_State hLua, lua_Writer writer, pVOID data, int strip) => _lua_dump(hLua, writer, data, strip);
        internal static int lua_error(lua_State hLua) => _lua_error(hLua);
        internal static int lua_gc(lua_State hLua, int what, int data) => _lua_gc(hLua, what, data);
        internal static int lua_gc(lua_State hLua, int what, int data, int data2) => _lua_gc_ex(hLua, what, data, data2);
        internal static lua_Alloc lua_getallocf(lua_State hLua, out pVOID ud) => _lua_getallocf(hLua, out ud);
        internal static int lua_getfield(lua_State hLua, int index, string k)
        {
            if (TryGetUTF8WithNullTerminator(k, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    return _lua_getfield(hLua, index, (pCHAR)p0);
            }
            return 0;
        }
        internal static lua_Hook lua_gethook(lua_State hLua) => _lua_gethook(hLua);
        internal static int lua_gethookcount(lua_State hLua) => _lua_gethookcount(hLua);
        internal static int lua_gethookmask(lua_State hLua) => _lua_gethookmask(hLua);
        internal static int lua_geti(lua_State hLua, int index, long i) => _lua_geti(hLua, index, i);
        internal static int lua_getinfo(lua_State hLua, string what, ref lua_Debug ar)
        {
            if (TryGetUTF8WithNullTerminator(what, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    return _lua_getinfo(hLua, (pCHAR)p0, ref ar);
            }
            return 0;
        }
        internal static int lua_getiuservalue(lua_State hLua, int idx, int n) => _lua_getiuservalue(hLua, idx, n);
        internal static pCHAR lua_getlocal(lua_State hLua, ref lua_Debug ar, int n) => _lua_getlocal(hLua, ref ar, n);
        internal static int lua_getmetatable(lua_State hLua, int index) => _lua_getmetatable(hLua, index);
        internal static int lua_getstack(lua_State hLua, int level, ref lua_Debug n) => _lua_getstack(hLua, level, ref n);
        internal static int lua_gettable(lua_State hLua, int index) => _lua_gettable(hLua, index);
        internal static int lua_gettop(lua_State hLua) => _lua_gettop(hLua);
        internal static pCHAR lua_getupvalue(lua_State hLua, int funcIndex, int n) => _lua_getupvalue(hLua, funcIndex, n);
        internal static int lua_iscfunction(lua_State hLua, int index) => _lua_iscfunction(hLua, index);
        internal static int lua_isinteger(lua_State hLua, int index) => _lua_isinteger(hLua, index);
        internal static int lua_isnumber(lua_State hLua, int index) => _lua_isnumber(hLua, index);
        internal static int lua_isstring(lua_State hLua, int index) => _lua_isstring(hLua, index);
        internal static int lua_isuserdata(lua_State hLua, int index) => _lua_isuserdata(hLua, index);
        internal static int lua_isyieldable(lua_State hLua) => _lua_isyieldable(hLua);
        internal static void lua_len(lua_State hLua, int index) => _lua_len(hLua, index);
        internal static int lua_load(lua_State hLua, lua_Reader reader, pVOID data, string chunkName, string mode)
        {
            if (TryGetUTF8WithNullTerminator(chunkName, out var chunkNameBytes) &&
                TryGetUTF8WithNullTerminator(mode, out var modeBytes))
            {
                fixed (byte* p0 = chunkNameBytes, p1 = modeBytes)
                    return _lua_load(hLua, reader, data, (pCHAR)p0, (pCHAR)p1);
            }
            return 0;
        }
        internal static lua_State lua_newstate(lua_Alloc allocFunction, pVOID ud) => _lua_newstate(allocFunction, ud);
        internal static lua_State lua_newthread(lua_State hLua) => _lua_newthread(hLua);
        internal static pVOID lua_newuserdatauv(lua_State hLua, pSIZE size, int nuvalue) => _lua_newuserdatauv(hLua, size, nuvalue);
        internal static int lua_next(lua_State hLua, int index) => _lua_next(hLua, index);
        internal static LuaStatus lua_pcall(lua_State hLua, int nargs, int nresults, int errorfunc) => _lua_pcall(hLua, nargs, nresults, errorfunc);
        internal static void lua_pushboolean(lua_State hLua, int value) => _lua_pushboolean(hLua, value);
        internal static void lua_pushcclosure(lua_State hLua, lua_CFunction f, int n) => _lua_pushcclosure(hLua, f, n);
        internal static void lua_pushinteger(lua_State hLua, lua_Integer n) => _lua_pushinteger(hLua, n);
        internal static void lua_pushlightuserdata(lua_State hLua, pVOID udata) => _lua_pushlightuserdata(hLua, udata);
        internal static void lua_pushansi(lua_State hLua, string s)
        {
            var p0 = Marshal.StringToHGlobalAnsi(s);
            _lua_pushstring(hLua, (pCHAR)p0);
            Marshal.FreeHGlobal(p0);
        }
        internal static pCHAR lua_pushlstring(lua_State hLua, pVOID s, pSIZE len) => _lua_pushlstring(hLua, s, len);
        internal unsafe static void lua_pushlstring(lua_State hLua, byte* p0, int len) => _lua_pushlstring(hLua, (pVOID)p0, (pSIZE)len);
        internal unsafe static void lua_pushlstring(lua_State hLua, ref byte ref0, int len)
        {
            fixed (byte* p0 = &ref0)
                _lua_pushlstring(hLua, (pVOID)p0, (pSIZE)len);
        }
        internal static void lua_pushnil(lua_State hLua) => _lua_pushnil(hLua);
        internal static void lua_pushnumber(lua_State hLua, lua_Number number) => _lua_pushnumber(hLua, number);
        internal static int lua_pushthread(lua_State hLua) => _lua_pushthread(hLua);
        internal static void lua_pushvalue(lua_State hLua, int index) => _lua_pushvalue(hLua, index);
        internal static int lua_rawequal(lua_State hLua, int index1, int index2) => _lua_rawequal(hLua, index1, index2);
        internal static int lua_rawget(lua_State hLua, int index) => _lua_rawget(hLua, index);
        internal static int lua_rawgeti(lua_State hLua, int index, lua_Integer n) => _lua_rawgeti(hLua, index, n);
        internal static int lua_rawgetp(lua_State hLua, int index, pVOID p) => _lua_rawgetp(hLua, index, p);
        internal static pSIZE lua_rawlen(lua_State hLua, int index) => _lua_rawlen(hLua, index);
        internal static void lua_rawset(lua_State hLua, int index) => _lua_rawset(hLua, index);
        internal static void lua_rawseti(lua_State hLua, int index, lua_Integer i) => _lua_rawseti(hLua, index, i);
        internal static void lua_rawsetp(lua_State hLua, int index, pVOID p) => _lua_rawsetp(hLua, index, p);
        internal static int lua_resetthread(lua_State hLua) => _lua_resetthread(hLua);
        internal static int lua_resume(lua_State hLua, lua_State from, int nargs, out int results) => _lua_resume(hLua, from, nargs, out results);
        internal static void lua_rotate(lua_State hLua, int index, int n) => _lua_rotate(hLua, index, n);
        internal static void lua_setallocf(lua_State hLua, lua_Alloc f, pVOID ud) => _lua_setallocf(hLua, f, ud);
        internal static void lua_setfield(lua_State hLua, int index, string key)
        {
            if (TryGetUTF8WithNullTerminator(key, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    _lua_setfield(hLua, index, (pCHAR)p0);
            }
        }
        internal static void lua_sethook(lua_State hLua, lua_Hook f, int mask, int count) => _lua_sethook(hLua, f, mask, count);
        internal static void lua_seti(lua_State hLua, int index, long n) => _lua_seti(hLua, index, n);
        internal static void lua_setiuservalue(lua_State hLua, int index, int n) => _lua_setiuservalue(hLua, index, n);
        internal static pCHAR lua_setlocal(lua_State hLua, ref lua_Debug ar, int n) => _lua_setlocal(hLua, ref ar, n);
        internal static void lua_setmetatable(lua_State hLua, int objIndex) => _lua_setmetatable(hLua, objIndex);
        internal static void lua_settable(lua_State hLua, int index) => _lua_settable(hLua, index);
        internal static void lua_settop(lua_State hLua, int newTop) => _lua_settop(hLua, newTop);
        internal static pCHAR lua_setupvalue(lua_State hLua, int funcIndex, int n) => _lua_setupvalue(hLua, funcIndex, n);
        internal static void lua_setwarnf(lua_State hLua, lua_WarnFunction warningFunctionPtr, pVOID ud) => _lua_setwarnf(hLua, warningFunctionPtr, ud);
        internal static int lua_status(lua_State hLua) => _lua_status(hLua);
        internal static pSIZE lua_stringtonumber(lua_State hLua, string s)
        {
            if (TryGetUTF8WithNullTerminator(s, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    return _lua_stringtonumber(hLua, (pCHAR)p0);
            }
            return 0;
        }
        internal static int lua_toboolean(lua_State hLua, int index) => _lua_toboolean(hLua, index);
        internal static lua_CFunction lua_tocfunction(lua_State hLua, int index) => _lua_tocfunction(hLua, index);
        internal static lua_CFunction lua_toclose(lua_State hLua, int index) => _lua_toclose(hLua, index);
        internal static lua_Integer lua_tointegerx(lua_State hLua, int index, out int isNum) => _lua_tointegerx(hLua, index, out isNum);
        internal unsafe static void lua_tolstring(lua_State hLua, int index, out byte* p, out int l)
        {
            p = (byte*)_lua_tolstring(hLua, index, out pSIZE len);
            l = (int)len;
        }
        internal static lua_Number lua_tonumberx(lua_State hLua, int index, out int isNum) => _lua_tonumberx(hLua, index, out isNum);
        internal static pVOID lua_topointer(lua_State hLua, int index) => _lua_topointer(hLua, index);
        internal static lua_State lua_tothread(lua_State hLua, int index) => _lua_tothread(hLua, index);
        internal static pVOID lua_touserdata(lua_State hLua, int index) => _lua_touserdata(hLua, index);
        internal static int lua_type(lua_State hLua, int index) => _lua_type(hLua, index);
        internal static pCHAR lua_typename(lua_State hLua, int type) => _lua_typename(hLua, type);
        internal static pVOID lua_upvalueid(lua_State hLua, int funcIndex, int n) => _lua_upvalueid(hLua, funcIndex, n);
        internal static void lua_upvaluejoin(lua_State hLua, int funcIndex1, int n1, int funcIndex2, int n2) => _lua_upvaluejoin(hLua, funcIndex1, n1, funcIndex2, n2);
        internal static lua_Number lua_version(lua_State hLua) => _lua_version(hLua);
        internal static void lua_warning(lua_State hLua, string msg, int tocont)
        {
            if (TryGetUTF8WithNullTerminator(msg, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    _lua_warning(hLua, (pCHAR)p0, tocont);
            }
        }
        internal static void lua_xmove(lua_State from, lua_State to, int n) => _lua_xmove(from, to, n);
        internal static int lua_yieldk(lua_State hLua, int nresults, lua_KContext ctx, lua_KFunction k) => _lua_yieldk(hLua, nresults, ctx, k);
        internal static int luaL_argerror(lua_State hLua, int arg, string message)
        {
            if (TryGetUTF8WithNullTerminator(message, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    return _luaL_argerror(hLua, arg, (pCHAR)p0);
            }
            return 0;
        }
        internal static int luaL_callmeta(lua_State hLua, int obj, string e)
        {
            if (TryGetUTF8WithNullTerminator(e, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    return _luaL_callmeta(hLua, obj, (pCHAR)p0);
            }
            return 0;
        }
        internal static void luaL_checkany(lua_State hLua, int arg) => _luaL_checkany(hLua, arg);
        internal static lua_Integer luaL_checkinteger(lua_State hLua, int arg) => _luaL_checkinteger(hLua, arg);
        internal static pCHAR luaL_checklstring(lua_State hLua, int arg, out pSIZE len) => _luaL_checklstring(hLua, arg, out len);
        internal static lua_Number luaL_checknumber(lua_State hLua, int arg) => _luaL_checknumber(hLua, arg);
        internal static int luaL_checkoption(lua_State hLua, int arg, string def, string[] list)
        {
            // Not yet supported 
            //_luaL_checkoption(hLua, arg, def, list);
            return 0;
        }
        internal static void luaL_checkstack(lua_State hLua, int sz, string message)
        {
            if (TryGetUTF8WithNullTerminator(message, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    _luaL_checkstack(hLua, sz, (pCHAR)p0);
            }
        }
        internal static void luaL_checktype(lua_State hLua, int arg, int type) => _luaL_checktype(hLua, arg, type);
        internal static pVOID luaL_checkudata(lua_State hLua, int arg, string tName)
        {
            if (TryGetUTF8WithNullTerminator(tName, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    return _luaL_checkudata(hLua, arg, (pCHAR)p0);
            }
            return 0;
        }
        internal static void luaL_checkversion_(lua_State hLua, lua_Number ver, pSIZE sz) => _luaL_checkversion_(hLua, ver, sz);
        internal static int luaL_error(lua_State hLua, string message)
        {
            if (TryGetUTF8WithNullTerminator(message, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    return _luaL_error(hLua, (pCHAR)p0);
            }
            return 0;
        }
        internal static int luaL_execresult(lua_State hLua, int stat) => _luaL_execresult(hLua, stat);
        internal static int luaL_fileresult(lua_State hLua, int stat, string fileName)
        {
            if (TryGetUTF8WithNullTerminator(fileName, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    return _luaL_fileresult(hLua, stat, (pCHAR)p0);
            }
            return 0;
        }
        internal static int luaL_getmetafield(lua_State hLua, int obj, string e)
        {
            if (TryGetUTF8WithNullTerminator(e, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    return _luaL_getmetafield(hLua, obj, (pCHAR)p0);
            }
            return 0;
        }
        internal static int luaL_getsubtable(lua_State hLua, int index, string name)
        {
            if (TryGetUTF8WithNullTerminator(name, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    return _luaL_getsubtable(hLua, index, (pCHAR)p0);
            }
            return 0;
        }
        internal static lua_Integer luaL_len(lua_State hLua, int index) => _luaL_len(hLua, index);
        internal static int luaL_loadbufferx(lua_State hLua, byte[] buff, pSIZE sz, string name, string mode)
        {
            if (TryGetUTF8WithNullTerminator(name, out var b1) &&
                TryGetUTF8WithNullTerminator(mode, out var b2))
            {
                fixed (byte* p0 = b1, p1 = b2)
                    return _luaL_loadbufferx(hLua, buff, sz, (pCHAR)p0, (pCHAR)p1);
            }
            return 0;
        }
        internal static int luaL_loadfile(lua_State hLua, string name)
        {
            // 파일 관련한 작업은 ANSI 형태로 처리해야 내부적으로 문제가 없는듯
            var p0 = Marshal.StringToHGlobalAnsi(name);
            var r = _luaL_loadfile(hLua, (pCHAR)p0);
            Marshal.FreeHGlobal(p0);
            return r;
        }
        internal static int luaL_newmetatable(lua_State hLua, string name)
        {
            if (TryGetUTF8WithNullTerminator(name, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    return _luaL_newmetatable(hLua, (pCHAR)p0);
            }
            return 0;
        }
        internal static lua_State luaL_newstate() => _luaL_newstate();
        internal static void luaL_openlibs(lua_State hLua) => _luaL_openlibs(hLua);
        internal static lua_Integer luaL_optinteger(lua_State hLua, int arg, lua_Integer d) => _luaL_optinteger(hLua, arg, d);
        internal static lua_Number luaL_optnumber(lua_State hLua, int arg, lua_Number d) => _luaL_optnumber(hLua, arg, d);
        internal static int luaL_ref(lua_State hLua, int registryIndex) => _luaL_ref(hLua, registryIndex);
        internal static void luaL_requiref(lua_State hLua, string moduleName, lua_CFunction openFunction, int global)
        {
            if (TryGetUTF8WithNullTerminator(moduleName, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    _luaL_requiref(hLua, (pCHAR)p0, openFunction, global);
            }
        }
        internal static void luaL_setmetatable(lua_State hLua, string tName)
        {
            if (TryGetUTF8WithNullTerminator(tName, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    _luaL_setmetatable(hLua, (pCHAR)p0);
            }
        }
        internal static pVOID luaL_testudata(lua_State hLua, int arg, string tName)
        {
            if (TryGetUTF8WithNullTerminator(tName, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    return _luaL_testudata(hLua, arg, (pCHAR)p0);
            }
            return 0;
        }
        internal static pCHAR luaL_traceback(lua_State hLua, lua_State luaState2, string message, int level)
        {
            if (TryGetUTF8WithNullTerminator(message, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    return _luaL_traceback(hLua, luaState2, (pCHAR)p0, level);
            }
            return 0;
        }
        internal static int luaL_typeerror(lua_State hLua, int arg, string typeName)
        {
            if (TryGetUTF8WithNullTerminator(typeName, out var bytes))
            {
                fixed (byte* p0 = bytes)
                    return _luaL_typeerror(hLua, arg, (pCHAR)p0);
            }
            return 0;
        }
        internal static void luaL_unref(lua_State hLua, int registryIndex, int reference) => _luaL_unref(hLua, registryIndex, reference);
        internal static void luaL_where(lua_State hLua, int level) => _luaL_where(hLua, level);


        private static bool TryGetUTF8WithNullTerminator(string s, out byte[] result)
        {
            if (s != null && s.Length != 0)
            {
                result = new byte[Encoding.UTF8.GetByteCount(s) + 2];
                Encoding.UTF8.GetBytes(s, result);
                return true;
            }
            result = null;
            return false;
        }
    }
}