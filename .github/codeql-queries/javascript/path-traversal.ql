/**
 * @name Path Traversal Vulnerability
 * @description Detects potential path traversal vulnerabilities
 * @kind problem
 * @severity error
 * @precision medium
 * @tags security, path-traversal, injection
 */

import javascript

// Detect path.join with user input
from CallExpr c
where c.getCallee() instanceof PropertyAccessExpr and
      c.getCallee().getPropertyName() = "join" and
      c.getCallee().getBase() instanceof MemberAccessExpr and
      c.getCallee().getBase().getPropertyName() = "path" and
      c.getArguments().size() > 1 and
      c.getArguments().get(1) instanceof DataFlow::Node
select c, "Potential path traversal: path.join with user-controlled input."

// Detect fs.readFile with user input
from CallExpr c
where c.getCallee() instanceof PropertyAccessExpr and
      c.getCallee().getPropertyName() = "readFile" and
      c.getCallee().getBase() instanceof MemberAccessExpr and
      c.getCallee().getBase().getPropertyName() = "fs" and
      c.getArguments().size() > 0 and
      c.getArguments().get(0) instanceof DataFlow::Node
select c, "Potential path traversal: fs.readFile with user-controlled path."

// Detect fs.writeFile with user input
from CallExpr c
where c.getCallee() instanceof PropertyAccessExpr and
      c.getCallee().getPropertyName() = "writeFile" and
      c.getCallee().getBase() instanceof MemberAccessExpr and
      c.getCallee().getBase().getPropertyName() = "fs" and
      c.getArguments().size() > 0 and
      c.getArguments().get(0) instanceof DataFlow::Node
select c, "Potential path traversal: fs.writeFile with user-controlled path."

// Detect path.resolve with user input
from CallExpr c
where c.getCallee() instanceof PropertyAccessExpr and
      c.getCallee().getPropertyName() = "resolve" and
      c.getCallee().getBase() instanceof MemberAccessExpr and
      c.getCallee().getBase().getPropertyName() = "path" and
      c.getArguments().size() > 1 and
      c.getArguments().get(1) instanceof DataFlow::Node
select c, "Potential path traversal: path.resolve with user-controlled input."

// Detect Express static with user input
from CallExpr c
where c.getCallee() instanceof PropertyAccessExpr and
      c.getCallee().getPropertyName() = "static" and
      c.getCallee().getBase() instanceof MemberAccessExpr and
      c.getCallee().getBase().getPropertyName() = "express" and
      c.getArguments().size() > 0 and
      c.getArguments().get(0) instanceof DataFlow::Node
select c, "Potential path traversal: express.static with user-controlled directory."