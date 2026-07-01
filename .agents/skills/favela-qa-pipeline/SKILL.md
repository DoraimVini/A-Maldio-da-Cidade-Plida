---
name: favela-qa-pipeline
description: Mandatory QA pipeline for compiling, testing, and committing changes in the Favela Amarela project. Triggered after any code alteration.
---

# Favela QA Pipeline (Mandatory)

Whenever a C# file or asset is altered, you MUST execute the following pipeline strictly in order. Do NOT advance to any other task or module until this cycle closes with success.

1. **SAVE**: Ensure the altered file is saved (handled inherently when writing tools succeed).
2. **COMPILE**: Verify there are no errors. Run `dotnet build` on the appropriate `.csproj` (e.g., `FavelaAmarela.Runtime.csproj` ou `FavelaAmarela.Tests.EditMode.csproj`) to validate compilation logic quickly.
3. **TEST**: Execute the tests related to the changed class. Use the PowerShell script `c:\Users\Vini\Desktop\projeto_amarelo\tools\run_qa_tests.ps1` to run Unity EditMode tests. Check the results carefully.
4. **SUCCESS -> COMMIT**: Se os testes passarem (SUCCESS), execute `git add .` (ou adicione os arquivos específicos) e sugira/faça um commit com uma mensagem curta, direta e profissional: `git commit -m "<mensagem>"`.
5. **FAILURE -> FIX**: Se houver falha de compilação ou de testes (FAILURE), corrija imediatamente o erro na respectiva classe e volte ao Passo 1. NUNCA pule etapas se a build ou o teste falhar.
