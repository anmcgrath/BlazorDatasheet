using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Protection;

/// <summary>
/// Declares a command's protection requirements. Implementations must be side-effect free
/// and check every region and operation they will modify using Sheet.Protection.
/// Commands without a declaration are denied while protection is enforced.
/// </summary>
public interface IProtectedCommand
{
    bool CanExecuteProtected(Sheet sheet);
}
