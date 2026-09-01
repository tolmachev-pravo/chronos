using Newtonsoft.Json;
using System;

namespace Chronos.Infrastructure.Jira.Dto
{
    public partial class DevStatusDetailDto
    {
        [JsonProperty("errors")]
        public object[] Errors { get; set; }

        [JsonProperty("detail")]
        public Detail[]? Detail { get; set; }
    }

    public partial class Detail
    {
        [JsonProperty("branches")]
        public Branch[] Branches { get; set; }

        [JsonProperty("pullRequests")]
        public PullRequest[] PullRequests { get; set; }

        [JsonProperty("repositories")]
        public object[] Repositories { get; set; }

        [JsonProperty("_instance")]
        public Instance Instance { get; set; }
    }

    public partial class Branch
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("createPullRequestUrl")]
        public Uri CreatePullRequestUrl { get; set; }

        [JsonProperty("repository")]
        public Repository Repository { get; set; }
    }

    public partial class Repository
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }

    public partial class Instance
    {
        [JsonProperty("singleInstance")]
        public bool SingleInstance { get; set; }

        [JsonProperty("baseUrl")]
        public Uri BaseUrl { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("typeName")]
        public string TypeName { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public partial class PullRequest
    {
        [JsonProperty("author")]
        public Author Author { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("commentCount")]
        public long CommentCount { get; set; }

        [JsonProperty("source")]
        public Destination Source { get; set; }

        [JsonProperty("destination")]
        public Destination Destination { get; set; }

        [JsonProperty("reviewers")]
        public Reviewer[] Reviewers { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("lastUpdate")]
        public string LastUpdate { get; set; }
    }

    public partial class Author
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("avatar")]
        public Uri Avatar { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }

    public partial class Destination
    {
        [JsonProperty("branch")]
        public string Branch { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }

    public partial class Reviewer
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("avatar")]
        public Uri Avatar { get; set; }

        [JsonProperty("approved")]
        public bool Approved { get; set; }
    }
}
