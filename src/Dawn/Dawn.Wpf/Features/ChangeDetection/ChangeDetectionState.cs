namespace Dawn.Wpf
{
    public enum ChangeDetectionState
    {
        // 0
        None = 0 << 0,

        // 1
        Identical = 1 << 0,

        // 2
        Changed = 1 << 1,

        // 4
        Missing = 1 << 2,
    }
}
