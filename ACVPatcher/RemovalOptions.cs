
class RemovalOptions
{
    public IEnumerable<string>? ActivitiesToRemove { get; set; }
    public IEnumerable<string>? PermissionsToRemove { get; set; }
    public IEnumerable<string>? ProvidersToRemove { get; set; }
    public bool HasRemovals()
    {
        return (ActivitiesToRemove != null && ActivitiesToRemove.Any()) ||
               (PermissionsToRemove != null && PermissionsToRemove.Any()) ||
               (ProvidersToRemove != null && ProvidersToRemove.Any());
    }
}
