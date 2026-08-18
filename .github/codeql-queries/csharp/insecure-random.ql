/**
 * @name Insecure Random Number Generator
 * @description Detects usage of insecure random number generators
 * @kind problem
 * @severity warning
 * @precision high
 * @tags security, randomness, cryptography
 */

import csharp

from Random r, MethodAccess ma
where ma.getMethod().hasName("Next") and
      ma.getQualifier().getType() = r.getType()
select ma, "Insecure random number generator 'Random.Next()' used. Consider using 'RandomNumberGenerator' for cryptographic purposes."

// Also detect System.Random usage in security-sensitive contexts
from VariableDeclaration vd, Type t
where t.hasName("Random") and
      vd.getType() = t and
      not vd.getParent().getParent().(MethodDeclaration).hasModifier("private")
select vd, "Instance of 'System.Random' created. Consider using 'RandomNumberGenerator' for cryptographic operations."