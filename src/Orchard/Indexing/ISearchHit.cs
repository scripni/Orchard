﻿using System;
namespace Orchard.Indexing {
    public interface ISearchHit {
        int ContentItemId { get; }
        float Score { get; }

        int GetInt(string name);
        float GetFloat(string name);
        bool GetBoolean(string name);
        string GetString(string name);
        DateTime GetDateTime(string name);
    }
}
