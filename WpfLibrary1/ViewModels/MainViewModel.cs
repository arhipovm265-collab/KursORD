using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using WpfLibrary1;
using WpfLibrary1.Data;

namespace WpfLibrary1.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ORDContext? _context;
        private readonly User? _currentUser;
        private readonly int? _currentOfficerId;

        public ObservableCollection<CaseRecord> Cases { get; } = new ObservableCollection<CaseRecord>();
        private CaseRecord? _selectedCase;
        public CaseRecord? SelectedCase
        {
            get => _selectedCase;
            set
            {
                _selectedCase = value;
                OnPropertyChanged();
                ((RelayCommand)DeleteCaseCommand).RaiseCanExecuteChanged();
                ((RelayCommand)EditLeadOfficerCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ShowDetailsCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand DeleteCaseCommand { get; }
        public ICommand AddSampleCaseCommand { get; }
        public ICommand EditLeadOfficerCommand { get; }
        public ICommand ShowDetailsCommand { get; }

        public bool CanEditAll
        {
            get
            {
                var rn = _currentUser?.Role?.Name ?? string.Empty;
                if (string.IsNullOrWhiteSpace(rn)) return false;
                if (string.Equals(rn, "admin", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(rn, "administrator", StringComparison.OrdinalIgnoreCase)) return true;
                if (rn.IndexOf("admin", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                return false;
            }
        }
        public User? CurrentUser => _currentUser;

        public MainViewModel(ORDContext? context = null, User? currentUser = null)
        {
            _context = context;
            _currentUser = currentUser;
            if (_context != null && _currentUser != null && _currentUser.OfficerId.HasValue)
            {
                _currentOfficerId = _currentUser.OfficerId;
            }

            AddSampleCaseCommand = new RelayCommand(_ => AddSample(), _ => CanEditAll || _currentUser != null);
            DeleteCaseCommand = new RelayCommand(_ => DeleteSelected(), _ => CanDeleteSelected());
            EditLeadOfficerCommand = new RelayCommand(_ => EditLeadOfficer(), _ => SelectedCase != null && CanEditAll);
            ShowDetailsCommand = new RelayCommand(_ => ShowDetails(), _ => SelectedCase != null);

            if (_context != null)
            {
                try
                {
                    var cases = _context.CaseRecords
                        .Include(c => c.LeadOfficer)
                        .Include(c => c.Location)
                        .Include(c => c.LeadOfficer!.Department)
                        .OrderByDescending(c => c.CreatedAt)
                        .Take(500)
                        .ToList();

                    foreach (var c in cases)
                        Cases.Add(c);
                }
                catch
                {
                }
            }
            else
            {
            }
        }

        private bool CanDeleteSelected()
        {
            if (SelectedCase == null) return false;
            if (_currentUser == null) return false;
            if (CanEditAll) return true;
            return SelectedCase.LeadOfficerId.HasValue && _currentOfficerId.HasValue && SelectedCase.LeadOfficerId.Value == _currentOfficerId.Value;
        }

        private void DeleteSelected()
        {
            if (SelectedCase == null) return;
            try
            {
                if (_context != null)
                {
                    _context.CaseRecords.Remove(SelectedCase);
                    _context.SaveChanges();
                }
            }
            catch
            {
            }

            Cases.Remove(SelectedCase);
            SelectedCase = null;
            OnPropertyChanged(nameof(Cases));
        }

        private void AddSample()
        {
            var c = new CaseRecord
            {
                CaseNumber = "ORD-" + DateTime.UtcNow.Ticks,
                Title = "Новый случай",
                Description = "Добавлен из UI",
                CreatedAt = DateTime.UtcNow,
                StatusId = (int)CaseStatus.Open
            };
            if (_currentOfficerId.HasValue)
            {
                try
                {
                    if (_context != null)
                    {
                        var off = _context.Officers.FirstOrDefault(o => o.Id == _currentOfficerId.Value);
                        if (off != null && off.CanBeLead)
                            c.LeadOfficerId = _currentOfficerId.Value;
                    }
                }
                catch
                {
                }
            }

            if (_context != null)
            {
                _context.CaseRecords.Add(c);
                _context.SaveChanges();
                var fresh = _context.CaseRecords
                    .Include(x => x.LeadOfficer).ThenInclude(o => o.Department)
                    .Include(x => x.Location)
                    .FirstOrDefault(x => x.Id == c.Id);

                var toInsert = fresh ?? c;
                Cases.Insert(0, toInsert);
                SelectedCase = toInsert;
            }
            else
            {
                Cases.Insert(0, c);
                SelectedCase = c;
            }

            OnPropertyChanged(nameof(Cases));
        }

        private void EditLeadOfficer()
        {
            if (SelectedCase == null) return;
            if (!CanEditAll)
            {
                System.Windows.MessageBox.Show("Недостаточно прав для изменения ведущего. Только администраторы могут менять ведущего.", "Доступ запрещён", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            using var ctx = new ORDContext();
            int? currentLeadId = SelectedCase?.LeadOfficerId;
            var officers = ctx.Officers
                .Where(o => o.CanBeLead || (currentLeadId.HasValue && o.Id == currentLeadId.Value))
                .OrderBy(o => o.LastName)
                .ToList();

            var win = new System.Windows.Window()
            {
                Title = "Выберите ведущего",
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                Width = 420,
                Height = 160,
                ResizeMode = System.Windows.ResizeMode.NoResize
            };

            var panel = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(10) };
            var combo = new System.Windows.Controls.ComboBox { ItemsSource = officers, DisplayMemberPath = "FullName", Width = 360 };
            if (SelectedCase.LeadOfficerId.HasValue)
                combo.SelectedItem = officers.FirstOrDefault(o => o.Id == SelectedCase.LeadOfficerId.Value);

            var buttons = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new System.Windows.Thickness(0,10,0,0) };
            var btnCancel = new System.Windows.Controls.Button { Content = "Отмена", Width = 80, Margin = new System.Windows.Thickness(0,0,8,0) };
            var btnOk = new System.Windows.Controls.Button { Content = "ОК", Width = 80 };
            btnCancel.Click += (_, _) => win.DialogResult = false;
            btnOk.Click += (_, _) => win.DialogResult = true;
            buttons.Children.Add(btnCancel);
            buttons.Children.Add(btnOk);

            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Выберите нового ведущего:", Margin = new System.Windows.Thickness(0,0,0,6) });
            panel.Children.Add(combo);
            panel.Children.Add(buttons);
            win.Content = panel;

            var result = win.ShowDialog();
            if (result != true) return;

            var sel = combo.SelectedItem as Officer;
            if (sel == null) return;

            try
            {
                if (_context != null)
                {
                    var cr = _context.CaseRecords.FirstOrDefault(x => x.Id == SelectedCase.Id);
                    if (cr != null)
                    {
                        cr.LeadOfficerId = sel.Id;
                        _context.SaveChanges();
                        var fresh = _context.CaseRecords
                            .Include(x => x.LeadOfficer).ThenInclude(o => o.Department)
                            .Include(x => x.Location)
                            .FirstOrDefault(x => x.Id == cr.Id);
                        if (fresh != null)
                        {
                            var idx = Cases.IndexOf(SelectedCase);
                            if (idx >= 0) Cases[idx] = fresh;
                            SelectedCase = fresh;
                        }
                        OnPropertyChanged(nameof(Cases));
                    }
                }
            }
            catch
            {
            }
        }

        private void ShowDetails()
        {
            if (SelectedCase == null) return;
            var dlg = new CaseDetailsWindow(SelectedCase.Id, _currentUser);
            dlg.Owner = System.Windows.Application.Current.MainWindow;
            dlg.ShowDialog();
            if (_context != null)
            {
                var fresh = _context.CaseRecords
                    .Include(x => x.LeadOfficer).ThenInclude(o => o.Department)
                    .Include(x => x.Location)
                    .FirstOrDefault(x => x.Id == SelectedCase.Id);
                if (fresh != null)
                {
                    var idx = Cases.IndexOf(SelectedCase);
                    if (idx >= 0) Cases[idx] = fresh;
                    SelectedCase = fresh;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;
        public event EventHandler? CanExecuteChangedInternal;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChangedInternal?.Invoke(this, EventArgs.Empty);
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
