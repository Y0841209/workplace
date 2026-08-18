/**
 * @name Cross-Site Scripting (XSS) Vulnerability
 * @description Detects potential XSS vulnerabilities in JavaScript/TypeScript
 * @kind problem
 * @severity error
 * @precision medium
 * @tags security, xss, injection
 */

import javascript

// Detect innerHTML with user input
from AssignmentExpr a
where a.getOperator() = "=" and
      a.getTarget() instanceof MemberAccessExpr and
      a.getRhs() instanceof PropertyAccess and
      a.getLhs().getPropertyName() = "innerHTML" and
      a.getRhs().getBase() instanceof DataFlow::Node
select a, "Potential XSS: Assignment to innerHTML with user-controlled data."

// Detect document.write with user input
from CallExpr c
where c.getCallee() instanceof PropertyAccessExpr and
      c.getCallee().getPropertyName() = "write" and
      c.getCallee().getBase() instanceof MemberAccessExpr and
      c.getCallee().getBase().getPropertyName() = "document" and
      c.getArguments().size() > 0
select c, "Potential XSS: document.write() with user input."

// Detect dangerouslySetInnerHTML in React
from JSXAttribute a
where a.getName() = "dangerouslySetInnerHTML" and
      a.getValue() instanceof ObjectLiteral and
      a.getValue().(ObjectLiteral).getProperty("__html") instanceof DataFlow::Node
select a, "Potential XSS: dangerouslySetInnerHTML with user-controlled data."

// Detect eval() with user input
from CallExpr c
where c.getCallee() instanceof Identifier and
      c.getCallee().getName() = "eval" and
      c.getArguments().size() > 0
select c, "Use of eval() with user input - potential XSS."

// Detect setTimeout/setInterval with string
from CallExpr c
where (c.getCallee() instanceof Identifier and
       (c.getCallee().getName() = "setTimeout" or
        c.getCallee().getName() = "setInterval")) and
      c.getArguments().size() > 0 and
      c.getArguments().get(0) instanceof StringLiteral
select c, "setTimeout/setInterval with string argument - potential XSS."