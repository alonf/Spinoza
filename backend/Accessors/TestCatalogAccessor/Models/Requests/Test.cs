﻿using Newtonsoft.Json;

namespace Spinoza.Backend.Accessor.TestCatalog.Models.Requests
{

    public class Test
    {
        public string MessageType { get; set; } = "";
        public string Id { get; set; }
        public string Title { get; set; }
        public string SchemaVersion { get; set; }
        public string TestVersion { get; set; }
        public string PreviousVersionId { get; set; }
        public string AuthorId { get; set; }
        public string Description { get; set; }
        public Tag[] Tags { get; set; }
        public Question[] Questions { get; set; }

        public string Status { get; set; }
    }

    public class Tag
    {
        public string Name { get; set; }
        public string Status { get; set; }
    }

    public class Question
    {
        public string QuestionId { get; set; }
        public string Status { get; set; }
    }

   
}