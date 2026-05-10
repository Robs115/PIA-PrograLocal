using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class User : INotifyPropertyChanged
{
    private string username;
    private string password;
    private bool isAdmin;
    private bool isDirty;

    internal bool isLoading;

    public string Username
    {
        get => username;
        set
        {
            if (username != value)
            {
                username = value;
                OnPropertyChanged();

                if (!isLoading)
                    IsDirty = true;
            }
        }
    }

    public string Password
    {
        get => password;
        set
        {
            if (password != value)
            {
                password = value;
                OnPropertyChanged();

                if (!isLoading)
                    IsDirty = true;
            }
        }
    }


    public bool IsAdmin
    {
        get => isAdmin;
        set
        {
            if (isAdmin != value)
            {
                isAdmin = value;
                OnPropertyChanged();

                if (!isLoading)
                    IsDirty = true;
            }
        }
    }

    public bool IsDirty
    {
        get => isDirty;
        set
        {
            if (isDirty != value)
            {
                isDirty = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
