/**
 * @name Cross-Site Scripting (XSS) Vulnerability
 * @description Detects potential XSS vulnerabilities in C# code
 * @kind problem
 * @severity error
 * @precision medium
 * @tags security, xss, injection
 */

import csharp

// Detect unvalidated user input in HTML output
from MethodAccess ma, Parameter p
where ma.getMethod().hasName("Write") and
      ma.getMethod().getDeclaringType().hasName("HtmlHelper") and
      p = ma.getArgument(0) and
      not p.getType().hasName("HtmlString") and
      not p.getType().hasName("IHtmlContent")
select ma, "Potential XSS: Unvalidated user input written to HTML output."

// Detect ViewBag/ViewData usage without encoding
from Assignment a, VariableDeclaration vd
where a.getTarget() = vd and
      vd.getType().hasName("ViewBag") and
      not vd.getInitializer() instanceof HtmlString
select a, "Potential XSS: Unencoded value assigned to ViewBag."

// Detect ViewData/ViewBag assignment without encoding
from Assignment a, MemberAccess ma
where a.getTarget() = ma and
      ma.getMember().hasName("ViewData") or
      ma.getMember().hasName("ViewBag") and
      a.getSource() instanceof StringLiteral and
      not a.getSource() instanceof HtmlString
select a, "Potential XSS: Unencoded string assigned to ViewData/ViewBag."

// Detect Raw() usage with user input
from MethodAccess ma
where ma.getMethod().hasName("Raw") and
      ma.getMethod().getDeclaringType().hasName("HtmlHelper") and
      ma.getArgument(0) instanceof VariableAccess
select ma, "Potential XSS: HtmlHelper.Raw() used with variable input."

// Detect @Html.Raw with user input
from MethodAccess ma
where ma.getMethod().hasName("Raw") and
      ma.getArgument(0) instanceof VariableAccess and
      ma.getArgument(0).(VariableAccess).getVariable() instanceof Parameter
select ma, "Potential XSS: HtmlHelper.Raw() used with parameter input."