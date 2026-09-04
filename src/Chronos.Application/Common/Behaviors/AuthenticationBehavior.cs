using MediatR;
using Chronos.Application.Authentication;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Common.Behaviors
{
	/// <summary>
	/// The one place where a refusal by Jira becomes
	/// <see cref="JiraAuthenticationException"/>. Handlers used to catch the Jira
	/// client's AuthenticationException one by one and rethrow it as a plain Exception,
	/// which lost the stack and left the UI with nothing to act on. See issue #305.
	/// </summary>
	public class AuthenticationBehavior<TRequest, TResponse> :
		IPipelineBehavior<TRequest, TResponse>
		where TRequest : IRequest<TResponse>
	{
		public async Task<TResponse> Handle(
			TRequest request,
			RequestHandlerDelegate<TResponse> next,
			CancellationToken cancellationToken)
		{
			try
			{
				return await next();
			}
			catch (Exception exception) when (JiraAuthenticationException.Describes(exception))
			{
				throw new JiraAuthenticationException(exception);
			}
		}
	}
}
