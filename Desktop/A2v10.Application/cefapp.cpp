

#include "stdafx.h"

#include "cefapp.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

// virtual 
void CCefApplication::OnRegisterCustomSchemes(CefRawPtr<CefSchemeRegistrar> registrar)
{
	registrar->AddCustomScheme("client", true, false, false, true, false, false);
}

// static 
void CCefApplication::Init(HINSTANCE hInstance)
{
	CefEnableHighDPISupport();

	CefMainArgs args(hInstance);
	CefSettings settings;
	settings.single_process = false;
	settings.multi_threaded_message_loop = false;
	settings.no_sandbox = true;
	settings.remote_debugging_port = 5555; /// TODO

	static CefRefPtr<CCefApplication> app(new CCefApplication());

	CefInitialize(args, settings, app.get(), nullptr);
}
