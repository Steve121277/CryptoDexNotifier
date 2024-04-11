using System;

namespace CryptoDexNotifier
{
    public enum LOG_EVENT
    {
        ONERRORMSG = 0,
        ONDEBUGMSG = 1,
        ONNOTICEMSG = 2
    }

	public delegate void LogEvent(LOG_EVENT evt, object param);
}
