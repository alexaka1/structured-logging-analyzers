// Fixture: ContextualLoggerConstructor/MicrosoftCorrectContextType.cs
using Microsoft.Extensions.Logging;

namespace Comparison_ContextualLoggerConstructor_MicrosoftCorrectContextType
{
class A
{
	ILogger<A> _log;
	
	public A(ILogger<A> log)
	{
		_log = log;
	}
}
}
