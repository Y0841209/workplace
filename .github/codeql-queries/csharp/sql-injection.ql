/**
 * @name SQL Injection Vulnerability
 * @description Detects potential SQL injection vulnerabilities
 * @kind problem
 * @severity error
 * @precision high
 * @tags security, sql-injection, injection
 */

import csharp

// Detect string concatenation in SQL queries
from MethodAccess ma, Expression e
where ma.getMethod().hasName("Execute") and
      ma.getMethod().getDeclaringType().hasName("SqlCommand") and
      e = ma.getArgument(0) and
      e instanceof BinaryOperation and
      e.getOperator() = "+"
select ma, "Potential SQL injection: string concatenation in SqlCommand.Execute"

// Detect string interpolation in SQL
from InterpolatedStringExpression ise, MethodAccess ma
where ma.getMethod().hasName("Execute") and
      ma.getMethod().getDeclaringType().hasName("SqlCommand") and
      ma.getArgument(0) = ise
select ma, "Potential SQL injection: string interpolation in SqlCommand.Execute"

// Detect FromSqlRaw with string concatenation
from MethodAccess ma
where ma.getMethod().hasName("FromSqlRaw") and
      ma.getArgument(0) instanceof BinaryOperation and
      ma.getArgument(0).(BinaryOperation).getOperator() = "+"
select ma, "Potential SQL injection: string concatenation in FromSqlRaw"

// Detect ExecuteSqlRaw with concatenation
from MethodAccess ma
where ma.getMethod().hasName("ExecuteSqlRaw") and
      ma.getArgument(0) instanceof BinaryOperation and
      ma.getArgument(0).(BinaryOperation).getOperator() = "+"
select ma, "Potential SQL injection: string concatenation in ExecuteSqlRaw"

// Detect raw SQL with string.Format
from MethodAccess ma
where ma.getMethod().hasName("FromSqlRaw") and
      ma.getArgument(0) instanceof MethodAccess and
      ma.getArgument(0).(MethodAccess).getMethod().hasName("Format")
select ma, "Potential SQL injection: string.Format in FromSqlRaw"