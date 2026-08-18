/**
 * @name Hardcoded Credentials
 * @description Detects hardcoded credentials, API keys, connection strings
 * @kind problem
 * @severity error
 * @precision high
 * @tags security, credentials, secrets
 */

import csharp

// Detect hardcoded connection strings
from StringLiteral s, VariableDeclaration vd
where s.getValue().matches(".*(Server|Data Source|Database|User ID|Password|Uid|Pwd|Secret|Key|Token).*") and
      s.getValue().length() > 20 and
      vd.getInitializer() = s
select vd, "Possible hardcoded connection string or credential detected."

// Detect hardcoded API keys
from StringLiteral s, VariableDeclaration vd
where s.getValue().matches("(?i)(api[_-]?key|secret[_-]?key|access[_-]?token|auth[_-]?token).*") and
      s.getValue().length() > 10 and
      vd.getInitializer() = s
select vd, "Possible hardcoded API key or token detected."

// Detect hardcoded JWT secrets
from StringLiteral s, VariableDeclaration vd
where s.getValue().matches("(?i)(jwt[_-]?secret|signing[_-]?key).*") and
      s.getValue().length() > 10 and
      vd.getInitializer() = s
select vd, "Possible hardcoded JWT secret detected."

// Detect connection strings in configuration
from StringLiteral s, Attribute a
where a.getName().matches(".*(ConnectionString|DefaultConnection).*") and
      a.getArguments().get(0) = s and
      s.getValue().matches(".*(Password|Pwd|Secret|Key).*")
select s, "Connection string with credentials found in attribute."