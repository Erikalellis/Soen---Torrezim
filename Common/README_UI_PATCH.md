PATCH NOTES - UI Standardization

What changed:
- Added Common/BaseForm.cs: Base class with standard font, background and StatusStrip.
- Added Common/UIHelpers.cs: helper factory methods for common controls.
- Added Common/ComboItems.cs: centralized combo helper types.
- Converted several Forms created by code to inherit BaseForm and use BaseStatusLabel.
- Adjusted multiple Forms to reuse BaseForm's StatusStrip instead of creating new ones.
- Updated models and DAOs earlier to fix compilation issues.

Revert:
- To revert changes, inspect git diff and selectively checkout previous versions of modified files.
- Files added: Common/BaseForm.cs, Common/UIHelpers.cs, Common/ComboItems.cs, CONTRIBUTING_UI.md, Common/README_UI_PATCH.md
- Files modified: multiple Forms (ManutecaoVeiculo.cs, AgendamentoServicos.cs, CriacaoOrcamentos.cs, RegistroVendas.cs, Usuarios.cs, etc.)

Recommendation:
- Run the solution and open main Forms in Visual Studio Designer to adjust layouts if needed.
- If a Designer file shows errors after these changes, revert that Form or file and adapt manually to preserve InitializeComponent().
