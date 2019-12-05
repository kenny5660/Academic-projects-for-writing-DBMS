﻿using System.Linq;
using DataBaseType;

namespace IronySqlParser.AstNodes
{
    public class TransactionEndOptNode : SqlNode
    {
        public TransactionEndType TransactionEndType { get; set; }

        public override void CollectInfoFromChild () => TransactionEndType = ParseEnum<TransactionEndType>(ChildNodes.First().Tokens.First().Text);
    }
}
