﻿// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.04.18

using System.Collections.Generic;
using System.Linq.Expressions;
using Xtensive.Core;
using Xtensive.Storage.Rse;
using Xtensive.Storage.Rse.Expressions;
using Xtensive.Core.Tuples.Transform;

namespace Xtensive.Storage.Linq.Rewriters
{
  internal class ItemProjectorRewriter : TupleAccessRewriter
  {
    private List<int> groupMapping;
    private RecordSetHeader header;

    protected override Expression VisitMethodCall(MethodCallExpression mc)
    {
      if (mc.Object!=null) {
        if (mc.Object.Type==typeof (Record) && mc.Method.Name=="get_Item") {
          var groupIndex = (int) ((ConstantExpression) mc.Arguments[0]).Value;
          return Expression.Call(mc.Object, mc.Method, Expression.Constant(groupMapping.IndexOf(groupIndex)));
        }
        if (mc.Object!=null && mc.Object.NodeType==ExpressionType.Constant && mc.Object.Type==typeof (SegmentTransform) && mc.Method.Name=="Apply") {
          var segmentTransform = (SegmentTransform) ((ConstantExpression) mc.Object).Value;
          var offset = mappings.IndexOf(segmentTransform.Segment.Offset);
          var newTransformExpression = Expression.Constant(new SegmentTransform(segmentTransform.IsReadOnly, header.TupleDescriptor, new Segment<int>(offset, segmentTransform.Segment.Length)));
          return Expression.Call(newTransformExpression, mc.Method, mc.Arguments);
        }
      }
      return base.VisitMethodCall(mc);
    }

    public Expression Rewrite(Expression expression, List<int> mappings, List<int> groupMapping, RecordSetHeader header)
    {
      try {
        this.mappings = mappings;
        this.groupMapping = groupMapping;
        this.header = header;
        return Visit(expression);
      }
      finally {
        this.mappings = null;
        this.groupMapping = null;
        this.header = null;
      }
    }
  }
}