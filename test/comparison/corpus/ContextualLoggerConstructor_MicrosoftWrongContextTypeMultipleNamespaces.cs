// Fixture: ContextualLoggerConstructor/MicrosoftWrongContextTypeMultipleNamespaces.cs
using Microsoft.Extensions.Logging;

namespace Comparison_ContextualLoggerConstructor_MicrosoftWrongContextTypeMultipleNamespaces_X
{
	class A { }
}

namespace Comparison_ContextualLoggerConstructor_MicrosoftWrongContextTypeMultipleNamespaces_Y
{
	class A
	{
		ILogger<Comparison_ContextualLoggerConstructor_MicrosoftWrongContextTypeMultipleNamespaces_X.A> _log;
		
		public A(ILogger<Comparison_ContextualLoggerConstructor_MicrosoftWrongContextTypeMultipleNamespaces_X.A> log)
		{
			_log = log;
		}
	}
}
