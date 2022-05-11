using System;
using System.Linq.Expressions;

namespace AzureCost_to_LogAnalytics.Extensions
{
    public static class LambdaExtensions
    {
        public static string GetMemberName<T, TKey>(this Expression<Func<T, TKey>> selector)
        {
            var member = GetMemberInfo(selector);
            return member?.Member.Name;
        }

        private static MemberExpression GetMemberInfo<T, U>(Expression<Func<T, U>> expression)
        {
            MemberExpression memberExpr = expression.Body as MemberExpression;
            if (memberExpr == null)
            {
                if (expression.Body is UnaryExpression unaryExpr && unaryExpr.NodeType == ExpressionType.Convert)
                {
                    memberExpr = unaryExpr.Operand as MemberExpression;
                }
            }
            if (memberExpr != null)
                return memberExpr;

            throw new ArgumentException("Expression is not a member access", "expression");
        }
    }
}
