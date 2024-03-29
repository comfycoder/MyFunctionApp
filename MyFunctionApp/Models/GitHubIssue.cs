﻿using Newtonsoft.Json;
using System;

namespace MyFunctionApp.Models
{
    /// <summary>
    /// A partial representation of an issue object from the GitHub API
    /// </summary>
    public class GitHubIssue
    {
        [JsonProperty(PropertyName = "html_url")]
        public string Url { get; set; }

        public string Title { get; set; }

        [JsonProperty(PropertyName = "created_at")]
        public DateTime Created { get; set; }
    }
}
