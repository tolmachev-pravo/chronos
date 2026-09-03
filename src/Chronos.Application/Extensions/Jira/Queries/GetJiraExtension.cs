using MediatR;
using Chronos.Application.Extensions.Jira.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Extensions.Jira.Queries
{
    public class GetJiraExtension
    {
        public record Query(string Username) : IRequest<JiraExtensionDto>;

        public class Handler : IRequestHandler<Query, JiraExtensionDto>
        {
            private readonly IJiraExtensionProvider _provider;

            public Handler(IJiraExtensionProvider provider)
            {
                _provider = provider;
            }

            /// <summary>
            /// The "no stored extension means connected with the default settings" rule
            /// lives in the provider, so the event providers see the same state outside
            /// the MediatR pipeline. See issue #299.
            /// </summary>
            public Task<JiraExtensionDto> Handle(Query request, CancellationToken cancellationToken)
                => _provider.GetAsync(request.Username, cancellationToken);
        }
    }
}
