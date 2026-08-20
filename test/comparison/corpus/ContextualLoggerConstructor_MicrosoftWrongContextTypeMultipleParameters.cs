// Fixture: ContextualLoggerConstructor/MicrosoftWrongContextTypeMultipleParameters.cs
using Microsoft.Extensions.Logging;

namespace Comparison_ContextualLoggerConstructor_MicrosoftWrongContextTypeMultipleParameters
{
class A
{
	ILogger<B> _log;
	
	public A(int a, ILogger<B> log)
	{
		_log = log;
	}
}

class B { }
}
