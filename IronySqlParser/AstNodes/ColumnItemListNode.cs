﻿using System.Collections.Generic;

namespace IronySqlParser.AstNodes
{
    class ColumnItemListNode : SqlNode
    {
        public List<List<string>> IdList { get; set; }

        public override void CollectInfoFromChild()
        {
            var columnItemNodes = FindAllChildNodesByType<ColumnItemNode>();

            IdList = new List<List<string>>();

            foreach (var columnItemNode in columnItemNodes)
            {
                IdList.Add(columnItemNode.Id);
            }
        }
    }
}