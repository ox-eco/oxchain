namespace OX.Bapps
{
    public delegate void BappEventHandler();
    public delegate void BappEventHandler<TEventArgs>(TEventArgs e);
}
