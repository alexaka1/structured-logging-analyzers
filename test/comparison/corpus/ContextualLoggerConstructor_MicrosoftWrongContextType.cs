// Fixture: ContextualLoggerConstructor/MicrosoftWrongContextType.cs
using Microsoft.Extensions.Logging;

namespace Comparison_ContextualLoggerConstructor_MicrosoftWrongContextType
{
class A
{
	ILogger<B> _log;
	
	public A(ILogger<B> log)
	{
		_log = log;
	}
}

class B { }
}
