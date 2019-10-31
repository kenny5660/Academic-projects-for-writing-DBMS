﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronySqlParser.AstNodes
{
    class JoinChainOptNode : SqlNode
    {
        public JoinKind JoinKind { get; set; }
        public List<List<string>> IdList { get; set; }
        public JoinStatementNode JoinStatementNode { get; set; }

        public override void CollectInfoFromChild()
        {
            JoinKind = FindFirstChildNodeByType<JoinKindOptNode>().JoinKindOpt;
            IdList = FindFirstChildNodeByType<IdListNode>().IdList;
            JoinStatementNode = FindFirstChildNodeByType<JoinStatementNode>();
        }
    }
}
