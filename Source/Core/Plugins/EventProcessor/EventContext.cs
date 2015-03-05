﻿using System;
using System.Collections.Generic;
using Exceptionless.Core.Pipeline;
using Exceptionless.Core.Utility;
using Exceptionless.Core.Models;

namespace Exceptionless.Core.Plugins.EventProcessor {
    public class EventContext : ExtensibleObject, IPipelineContext {
        public EventContext(PersistentEvent ev) {
            Event = ev;
            StackSignatureData = new Dictionary<string, string>();
        }

        public PersistentEvent Event { get; set; }
        public Stack Stack { get; set; }
        public Project Project { get; set; }
        public Organization Organization { get; set; }
        public bool IsNew { get; set; }
        public bool IsRegression { get; set; }
        public string SignatureHash { get; set; }
        public IDictionary<string, string> StackSignatureData { get; private set; }

        public bool IsCancelled { get; set; }
        public bool IsProcessed { get; set; }

        public bool HasError {
            get { return ErrorMessage != null || Exception != null; }
        }

        public void SetError(string message, Exception ex = null) {
            ErrorMessage = message;
            Exception = ex;
        }

        public string ErrorMessage { get; private set; }
        public Exception Exception { get; private set; }
    }
}