using MediatR;
using System;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Issues.Queries
{
    public class GetIssueOpenPullRequestUrl
    {
        public class Query : IRequest<Model>
        {                           
            public string Identifier { get; set; }
        }

        public class Model
        {
            public string Url { get; set; }
        }

        public class QueryHandler : IRequestHandler<Query, Model>
        {
            private readonly IIssueDataSource _issueDataSource;

            public QueryHandler(IIssueDataSource issueDataSource)
            {
                _issueDataSource = issueDataSource;
            }

            public async Task<Model> Handle(
                Query query,
                CancellationToken cancellationToken)
            {
                try
                {
                    var url = await _issueDataSource.GetIssueOpenPullRequestUrlAsync(query, cancellationToken);
                    return new Model { Url = url };
                }
                catch (AuthenticationException e)
                {
                    throw new Exception($"Authentication exception") { Source = e.Source };
                }
            }
        }
    }
}
