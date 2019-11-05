﻿using System.Collections.Generic;

namespace IronySqlParser.AstNodes
{
    class ColumnSourceNode : SqlNode
    {
        public List<string> Id { get; set; }

        public override void CollectInfoFromChild() => Id = FindFirstChildNodeByType<IdNode>().Id;
    }
}