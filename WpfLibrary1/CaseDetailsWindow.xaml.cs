using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using WpfLibrary1.Data;
using System.IO;
using ClosedXML.Excel;
using System;

namespace WpfLibrary1
{
    public partial class CaseDetailsWindow : Window
    {
        private readonly int _caseId;
        private readonly User? _currentUser;
        private CaseRecord? _case;
        private int? _editingSuspectId = null;

        public CaseDetailsWindow(int caseId, User? currentUser)
        {
            InitializeComponent();
            _caseId = caseId;
            _currentUser = currentUser;

            LoadCase();
        }

        private void OnDeleteEvidence(object sender, RoutedEventArgs e)
        {
            if (_case == null) return;
            var sel = EvidenceGrid.SelectedItem as Evidence;
            if (sel == null)
            {
                MessageBox.Show("Выберите улику для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ok = MessageBox.Show($"Удалить улику '{sel.Tag}'?", "Подтвердите удаление", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ok != MessageBoxResult.Yes) return;

            try
            {
                using var ctx = new ORDContext();
                var dbEvi = ctx.Evidences.FirstOrDefault(x => x.Id == sel.Id);
                if (dbEvi != null)
                {
                    ctx.Evidences.Remove(dbEvi);
                    ctx.SaveChanges();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка удаления улики: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadCase();
        }

        private void OnLocationSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (LocationStreetBox.SelectedItem is Location sel)
            {
                LocationCityBox.Text = sel.City ?? string.Empty;
                LocationHouseBox.Text = sel.House ?? string.Empty;
            }
        }

        private void OnSuspectSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var sel = SuspectsGrid.SelectedItem as Suspect;
            if (sel == null)
            {
                _editingSuspectId = null;
                return;
            }

            _editingSuspectId = sel.Id;
            NewSuspectLastName.Text = sel.LastName ?? string.Empty;
            NewSuspectFirstName.Text = sel.FirstName ?? string.Empty;
            NewSuspectPatronymic.Text = sel.Patronymic ?? string.Empty;
            NewSuspectDob.SelectedDate = sel.DateOfBirth;
            NewSuspectAppearedInOtherCases.Text = sel.AppearedInOtherCasesText ?? "Нет";
        }

        private void OnDeleteSuspect(object sender, RoutedEventArgs e)
        {
            if (_case == null) return;
            var sel = SuspectsGrid.SelectedItem as Suspect;
            if (sel == null)
            {
                MessageBox.Show("Выберите подозреваемого для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ok = MessageBox.Show($"Удалить подозреваемого '{sel.FullName}'?", "Подтвердите удаление", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ok != MessageBoxResult.Yes) return;

            try
            {
                using var ctx = new ORDContext();
                var s = ctx.Suspects.Include(x => x.Cases).FirstOrDefault(x => x.Id == sel.Id);
                if (s != null)
                {
                    var cr = ctx.CaseRecords.Include(c => c.Suspects).FirstOrDefault(c => c.Id == _case.Id);
                    if (cr != null)
                    {
                        
                        if (s.Cases != null && s.Cases.Count > 1)
                        {
                            s.Cases.Remove(cr);
                        }
                        else
                        {
                            ctx.Suspects.Remove(s);
                        }
                        ctx.SaveChanges();
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка удаления подозреваемого: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadCase();
        }

        private void OnEditSuspect(object sender, RoutedEventArgs e)
        {
            if (_case == null) return;

            if (_editingSuspectId == null)
            {
                MessageBox.Show("Выберите подозреваемого в списке, затем нажмите 'Изменить подозреваемого'.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var last = NewSuspectLastName.Text?.Trim();
            var first = NewSuspectFirstName.Text?.Trim();
            var patronymic = string.IsNullOrWhiteSpace(NewSuspectPatronymic.Text) ? null : NewSuspectPatronymic.Text.Trim();
            var dob = NewSuspectDob.SelectedDate;

            if (string.IsNullOrEmpty(last) || string.IsNullOrEmpty(first))
            {
                MessageBox.Show("Введите фамилию и имя подозреваемого.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new ORDContext();
                var s = ctx.Suspects.FirstOrDefault(x => x.Id == _editingSuspectId.Value);
                if (s != null)
                {
                    s.LastName = last;
                    s.FirstName = first;
                    s.Patronymic = patronymic;
                    s.DateOfBirth = dob;
                    ctx.SaveChanges();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения подозреваемого: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _editingSuspectId = null;
            NewSuspectLastName.Text = string.Empty;
            NewSuspectFirstName.Text = string.Empty;
            NewSuspectPatronymic.Text = string.Empty;
            NewSuspectDob.SelectedDate = null;
            NewSuspectAppearedInOtherCases.Text = string.Empty;

            LoadCase();
        }


        private void OnAddSuspect(object sender, RoutedEventArgs e)
        {
            if (_case == null) return;
            var last = NewSuspectLastName.Text?.Trim();
            var first = NewSuspectFirstName.Text?.Trim();
            var patronymic = string.IsNullOrWhiteSpace(NewSuspectPatronymic.Text) ? null : NewSuspectPatronymic.Text.Trim();
            var dob = NewSuspectDob.SelectedDate;

            if (string.IsNullOrEmpty(last) || string.IsNullOrEmpty(first))
            {
                MessageBox.Show("Введите фамилию и имя подозреваемого.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new ORDContext();
                var cr = ctx.CaseRecords.Include(c => c.Suspects).FirstOrDefault(x => x.Id == _case.Id);
                if (cr == null) throw new System.Exception("Дело не найдено в базе.");

                var s = new Suspect
                {
                    FirstName = first,
                    Patronymic = patronymic,
                    LastName = last,
                    DateOfBirth = dob
                };

                if (cr.Suspects == null) cr.Suspects = new System.Collections.Generic.List<Suspect>();
                cr.Suspects.Add(s);
                ctx.SaveChanges();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка добавления подозреваемого: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadCase();
            
            NewSuspectLastName.Text = string.Empty;
            NewSuspectFirstName.Text = string.Empty;
            NewSuspectPatronymic.Text = string.Empty;
            NewSuspectDob.SelectedDate = null;
            NewSuspectAppearedInOtherCases.Text = string.Empty;
            _editingSuspectId = null;
        }

        private void LoadCase()
        {
            using var ctx = new ORDContext();
            var c = ctx.CaseRecords
                .Include(x => x.LeadOfficer).ThenInclude(o => o.Department)
                .Include(x => x.Location)
                .Include(x => x.Suspects)
                .Include(x => x.EvidenceItems).ThenInclude(e => e.CollectedBy)
                .FirstOrDefault(x => x.Id == _caseId);

            if (c == null)
            {
                MessageBox.Show("Дело не найдено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            _case = c;

            CaseNumberBox.Text = c.CaseNumber ?? string.Empty;
            TitleBox.Text = c.Title ?? string.Empty;
            DescriptionBox.Text = c.Description ?? string.Empty;

            
            StatusBox.ItemsSource = System.Enum.GetValues(typeof(CaseStatus)).Cast<CaseStatus>().Select(s => new { Id = (int)s, Name = s.ToString() }).ToList();
            StatusBox.SelectedValuePath = "Id";
            StatusBox.DisplayMemberPath = "Name";
            StatusBox.SelectedValue = c.StatusId;

            
            var locs = ctx.Locations.ToList();
            LocationStreetBox.ItemsSource = locs;
            var cities = locs.Select(l => l.City).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
            LocationCityBox.ItemsSource = cities;
            if (c.Location != null)
            {
                var match = locs.FirstOrDefault(l => l.Id == c.Location.Id) ?? locs.FirstOrDefault(l => (l.Street == c.Location.Street && l.House == c.Location.House));
                if (match != null) LocationStreetBox.SelectedItem = match;
                else LocationStreetBox.Text = c.Location.Street;
                LocationHouseBox.Text = c.Location.House ?? string.Empty;
                LocationCityBox.Text = c.Location.City ?? string.Empty;
            }
            else
            {
                LocationCityBox.Text = string.Empty;
            }


            CaseLead.Text = c.LeadOfficer?.FullName ?? "Не назначен";
            CaseLeadEmail.Text = c.LeadOfficer?.Email ?? string.Empty;
            CaseLeadPhone.Text = c.LeadOfficer?.Phone ?? string.Empty;

            var suspectsList = c.Suspects?.ToList() ?? new System.Collections.Generic.List<Suspect>();
            foreach (var s in suspectsList)
            {
                bool appeared = ctx.CaseRecords.Any(c => c.Id != _case.Id && c.Suspects.Any(x => x.Id == s.Id));
                s.AppearedInOtherCasesText = appeared ? "Да" : "Нет";
            }
            SuspectsGrid.ItemsSource = suspectsList;
            EvidenceGrid.ItemsSource = c.EvidenceItems?.ToList();

            
            _editingSuspectId = null;
            NewSuspectLastName.Text = string.Empty;
            NewSuspectFirstName.Text = string.Empty;
            NewSuspectPatronymic.Text = string.Empty;
            NewSuspectDob.SelectedDate = null;
            NewSuspectAppearedInOtherCases.Text = string.Empty;
            
            
            var officers = ctx.Officers.OrderBy(o => o.LastName).ToList();
            NewEvidenceCollector.ItemsSource = officers;
            NewEvidenceCollector.SelectedItem = officers.FirstOrDefault();
            int? currentOfficerId = _currentUser?.OfficerId;

            var canEdit = _currentUser != null && (_currentUser.Role?.Name == "Admin" || (c.LeadOfficerId.HasValue && currentOfficerId.HasValue && c.LeadOfficerId.Value == currentOfficerId.Value));
            TitleBox.IsReadOnly = !canEdit;
            DescriptionBox.IsReadOnly = !canEdit;
            StatusBox.IsEnabled = canEdit;
            LocationStreetBox.IsEnabled = canEdit;
            LocationHouseBox.IsEnabled = canEdit;
            LocationCityBox.IsEnabled = canEdit;
            AddEvidenceButton.IsEnabled = canEdit;
            EditEvidenceButton.IsEnabled = canEdit;
            var delBtn = this.FindName("DeleteEvidenceButton") as System.Windows.Controls.Button;
            if (delBtn != null) delBtn.IsEnabled = canEdit;
            EditLeadButton.IsEnabled = _currentUser != null && _currentUser.Role?.Name == "Admin";
            SaveButton.IsEnabled = canEdit;
            CaseNumberBox.IsReadOnly = !canEdit;
        }

        private void OnEditLead(object sender, RoutedEventArgs e)
        {
            if (_case == null) return;
            if (_currentUser == null || _currentUser.Role?.Name != "Admin")
            {
                MessageBox.Show("Недостаточно прав для изменения ведущего. Только администраторы могут менять ведущего.", "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var ctx = new ORDContext();
            int? currentLeadId = _case?.LeadOfficerId;
            var officers = ctx.Officers
                .Where(o => o.CanBeLead || (currentLeadId.HasValue && o.Id == currentLeadId.Value))
                .OrderBy(o => o.LastName)
                .ToList();

            var win = new Window()
            {
                Title = "Выберите ведущего",
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = 420,
                Height = 160,
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };
            var combo = new System.Windows.Controls.ComboBox { ItemsSource = officers, DisplayMemberPath = "FullName", Width = 360 };
            if (_case.LeadOfficerId.HasValue)
            {
                combo.SelectedItem = officers.FirstOrDefault(o => o.Id == _case.LeadOfficerId.Value);
            }

            var buttons = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new Thickness(0,10,0,0) };
            var btnCancel = new System.Windows.Controls.Button { Content = "Отмена", Width = 80, Margin = new Thickness(0,0,8,0) };
            var btnOk = new System.Windows.Controls.Button { Content = "ОК", Width = 80 };

            btnCancel.Click += (_, _) => win.DialogResult = false;
            btnOk.Click += (_, _) => win.DialogResult = true;

            buttons.Children.Add(btnCancel);
            buttons.Children.Add(btnOk);

            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Выберите нового ведущего:", Margin = new Thickness(0,0,0,6) });
            panel.Children.Add(combo);
            panel.Children.Add(buttons);

            win.Content = panel;

            var result = win.ShowDialog();
            if (result != true) return;

            var sel = combo.SelectedItem as Officer;
            if (sel == null) return;

            var cr = ctx.CaseRecords.FirstOrDefault(x => x.Id == _case.Id);
            if (cr != null)
            {
                cr.LeadOfficerId = sel.Id;
                ctx.SaveChanges();
            }
            LoadCase();
        }



        private void OnSave(object sender, RoutedEventArgs e)
        {
            if (_case == null) return;

            try
            {
                using var ctx = new ORDContext();
                var cr = ctx.CaseRecords.FirstOrDefault(x => x.Id == _case.Id);
                if (cr == null) return;

                var oldStatus = cr.StatusId;

                cr.Title = TitleBox.Text;
                cr.CaseNumber = CaseNumberBox.Text;
                cr.Description = DescriptionBox.Text;

                if (StatusBox.SelectedValue is int sid)
                {
                    if (sid != oldStatus)
                    {
                        cr.StatusId = sid;
                        ctx.CaseStatusHistories.Add(new CaseStatusHistory
                        {
                            CaseRecordId = cr.Id,
                            StatusId = sid,
                            ChangedAt = DateTime.UtcNow
                        });
                    }
                }

                var streetText = LocationStreetBox.Text?.Trim();
                var houseText = LocationHouseBox.Text?.Trim();
                var cityText = LocationCityBox.Text?.Trim();

                if (LocationStreetBox.SelectedItem is Location selectedLoc)
                {
                    var locToUpdate = ctx.Locations.FirstOrDefault(l => l.Id == selectedLoc.Id);
                    if (locToUpdate != null)
                    {
                        locToUpdate.Street = string.IsNullOrEmpty(streetText) ? locToUpdate.Street : streetText;
                        locToUpdate.House = string.IsNullOrEmpty(houseText) ? locToUpdate.House : houseText;
                        locToUpdate.City = string.IsNullOrEmpty(cityText) ? null : cityText;
                        ctx.SaveChanges();
                        cr.LocationId = locToUpdate.Id;
                    }
                    else
                    {
                        cr.LocationId = selectedLoc.Id;
                    }
                }
                else if (!string.IsNullOrEmpty(streetText))
                {
                    var newLoc2 = ctx.Locations.FirstOrDefault(l =>
                        l.Street == streetText &&
                        l.House == houseText &&
                        (l.City == cityText || (l.City == null && cityText == null)));
                    if (newLoc2 == null)
                    {
                        newLoc2 = new Location { Street = streetText, House = string.IsNullOrEmpty(houseText) ? null : houseText, City = string.IsNullOrEmpty(cityText) ? null : cityText };
                        ctx.Locations.Add(newLoc2);
                        ctx.SaveChanges();
                    }
                    cr.LocationId = newLoc2.Id;
                }

                cr.UpdatedAt = DateTime.UtcNow;

                ctx.SaveChanges();
                MessageBox.Show("Сохранено.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadCase();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnAddEvidence(object sender, RoutedEventArgs e)
        {
            if (_case == null) return;

            var tag = NewEvidenceTag.Text?.Trim();
            var desc = NewEvidenceDescription.Text?.Trim();
            var date = NewEvidenceDate.SelectedDate;
            var collector = NewEvidenceCollector.SelectedItem as Officer;

            if (string.IsNullOrEmpty(tag) && string.IsNullOrEmpty(desc))
            {
                MessageBox.Show("Укажите тег или описание улики.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new ORDContext();
                var evi = new Evidence
                {
                    Tag = tag ?? string.Empty,
                    Description = desc,
                    CollectedAt = date.HasValue ? date.Value : (System.DateTime?)null,
                    CaseRecordId = _case.Id,
                    CollectedByOfficerId = collector?.Id
                };
                ctx.Evidences.Add(evi);
                ctx.SaveChanges();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка добавления улики: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadCase();
            NewEvidenceTag.Text = string.Empty;
            NewEvidenceDescription.Text = string.Empty;
            NewEvidenceDate.SelectedDate = null;
            NewEvidenceCollector.SelectedItem = null;
        }

        private void OnEditEvidence(object sender, RoutedEventArgs e)
        {
            if (_case == null) return;
            var sel = EvidenceGrid.SelectedItem as Evidence;
            if (sel == null)
            {
                MessageBox.Show("Выберите улику для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var ctx = new ORDContext();
            var officers = ctx.Officers.OrderBy(o => o.LastName).ToList();

            var win = new Window()
            {
                Title = "Редактировать улику",
                Owner = this,
                Width = 620,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };
            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Тег:" });
            var tagBox = new System.Windows.Controls.TextBox { Text = sel.Tag, Width = 580, Margin = new Thickness(0,0,0,8) };
            panel.Children.Add(tagBox);

            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Описание:" });
            var descBox = new System.Windows.Controls.TextBox { Text = sel.Description, Width = 580, Height = 80, TextWrapping = System.Windows.TextWrapping.Wrap, AcceptsReturn = true, Margin = new Thickness(0,0,0,8) };
            panel.Children.Add(descBox);

            var h = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            var datePicker = new System.Windows.Controls.DatePicker { SelectedDate = sel.CollectedAt?.Date, Width = 150 };
            var timeBox = new System.Windows.Controls.TextBox { Text = sel.CollectedAt?.ToString("HH:mm:ss") ?? string.Empty, Width = 80, Margin = new Thickness(8,0,0,0) };
            var collectorBox = new System.Windows.Controls.ComboBox { ItemsSource = officers, DisplayMemberPath = "FullName", Width = 300, Margin = new Thickness(8,0,0,0) };
            if (sel.CollectedBy != null) collectorBox.SelectedItem = officers.FirstOrDefault(o => o.Id == sel.CollectedBy.Id);
            h.Children.Add(new System.Windows.Controls.TextBlock { Text = "Дата:" , VerticalAlignment = VerticalAlignment.Center});
            h.Children.Add(datePicker);
            h.Children.Add(new System.Windows.Controls.TextBlock { Text = "Время:", Margin = new Thickness(8,0,0,0), VerticalAlignment = VerticalAlignment.Center });
            h.Children.Add(timeBox);
            h.Children.Add(new System.Windows.Controls.TextBlock { Text = "Собрал:", Margin = new Thickness(8,0,0,0), VerticalAlignment = VerticalAlignment.Center });
            h.Children.Add(collectorBox);
            panel.Children.Add(h);

            var buttons = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new Thickness(0,12,0,0) };
            var btnCancel = new System.Windows.Controls.Button { Content = "Отмена", Width = 90, Margin = new Thickness(0,0,8,0) };
            var btnOk = new System.Windows.Controls.Button { Content = "Сохранить", Width = 100 };
            btnCancel.Click += (_, _) => win.DialogResult = false;
            btnOk.Click += (_, _) => win.DialogResult = true;
            buttons.Children.Add(btnCancel);
            buttons.Children.Add(btnOk);
            panel.Children.Add(buttons);

            win.Content = panel;
            var res = win.ShowDialog();
            if (res != true) return;

            try
            {
                using var ctx2 = new ORDContext();
                var evi = ctx2.Evidences.FirstOrDefault(x => x.Id == sel.Id);
                if (evi != null)
                {
                    evi.Tag = tagBox.Text;
                    evi.Description = descBox.Text;
                    
                    if (datePicker.SelectedDate.HasValue)
                    {
                        var d = datePicker.SelectedDate.Value;
                        if (System.TimeSpan.TryParse(timeBox.Text, out var t))
                            evi.CollectedAt = d.Date + t;
                        else
                            evi.CollectedAt = d.Date;
                    }
                    else
                    {
                        evi.CollectedAt = null;
                    }

                    var coll = collectorBox.SelectedItem as Officer;
                    evi.CollectedByOfficerId = coll?.Id;
                    ctx2.SaveChanges();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка редактирования улики: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadCase();
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnExport(object sender, RoutedEventArgs e)
        {
            if (_case == null)
            {
                MessageBox.Show("Дело не загружено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new ORDContext();
                var full = ctx.CaseRecords
                    .Include(x => x.LeadOfficer).ThenInclude(o => o.Rank)
                    .Include(x => x.LeadOfficer).ThenInclude(o => o.Department)
                    .Include(x => x.Location)
                    .Include(x => x.Suspects)
                    .Include(x => x.EvidenceItems).ThenInclude(e => e.CollectedBy)
                    .Include(x => x.StatusHistories)
                    .FirstOrDefault(x => x.Id == _case.Id);

                if (full == null)
                {
                    MessageBox.Show("Дело не найдено в базе.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var projectDir = FindProjectRoot() ?? AppContext.BaseDirectory;

                var safeCaseNumber = string.IsNullOrWhiteSpace(full.CaseNumber) ? full.Id.ToString() : string.Concat(full.CaseNumber.Split(Path.GetInvalidFileNameChars()));
                var filename = $"case_{safeCaseNumber}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                var fullPath = Path.Combine(projectDir, filename);

                using var wb = new XLWorkbook();

                var ws = wb.Worksheets.Add("Дело");
                ws.Cell(1, 1).Value = "Номер";
                ws.Cell(1, 2).Value = full.CaseNumber;
                ws.Cell(2, 1).Value = "Заголовок";
                ws.Cell(2, 2).Value = full.Title;
                ws.Cell(3, 1).Value = "Описание";
                ws.Cell(3, 2).Value = full.Description;
                ws.Cell(4, 1).Value = "Статус";
                ws.Cell(4, 2).Value = ((CaseStatus)full.StatusId).ToString();
                ws.Cell(5, 1).Value = "Дата создания";
                ws.Cell(5, 2).Value = full.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                ws.Cell(6, 1).Value = "Дата обновления";
                ws.Cell(6, 2).Value = full.UpdatedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;

                ws.Columns().AdjustToContents();

                var wsLead = wb.Worksheets.Add("Ведущий");
                wsLead.Cell(1, 1).Value = "Фамилия";
                wsLead.Cell(1, 2).Value = "Имя";
                wsLead.Cell(1, 3).Value = "Отчество";
                wsLead.Cell(1, 4).Value = "Звание";
                wsLead.Cell(1, 5).Value = "Отдел";
                wsLead.Cell(1, 6).Value = "Email";
                wsLead.Cell(1, 7).Value = "Телефон";
                wsLead.Cell(1, 8).Value = "Можно назначать ведущим";
                if (full.LeadOfficer != null)
                {
                    wsLead.Cell(2, 1).Value = full.LeadOfficer.LastName;
                    wsLead.Cell(2, 2).Value = full.LeadOfficer.FirstName;
                    wsLead.Cell(2, 3).Value = full.LeadOfficer.Patronymic;
                    wsLead.Cell(2, 4).Value = full.LeadOfficer.Rank?.Name ?? string.Empty;
                    wsLead.Cell(2, 5).Value = full.LeadOfficer.Department?.Name ?? string.Empty;
                    wsLead.Cell(2, 6).Value = full.LeadOfficer.Email ?? string.Empty;
                    wsLead.Cell(2, 7).Value = full.LeadOfficer.Phone ?? string.Empty;
                    wsLead.Cell(2, 8).Value = full.LeadOfficer.CanBeLead ? "Да" : "Нет";
                }
                wsLead.Columns().AdjustToContents();

                var wsLoc = wb.Worksheets.Add("Место");
                wsLoc.Cell(1, 1).Value = "Улица";
                wsLoc.Cell(1, 2).Value = "Дом";
                wsLoc.Cell(1, 3).Value = "Город";
                if (full.Location != null)
                {
                    wsLoc.Cell(2, 1).Value = full.Location.Street ?? string.Empty;
                    wsLoc.Cell(2, 2).Value = full.Location.House ?? string.Empty;
                    wsLoc.Cell(2, 3).Value = full.Location.City ?? string.Empty;
                }
                wsLoc.Columns().AdjustToContents();

                var wsSus = wb.Worksheets.Add("Подозреваемые");
                wsSus.Cell(1, 1).Value = "Фамилия";
                wsSus.Cell(1, 2).Value = "Имя";
                wsSus.Cell(1, 3).Value = "Отчество";
                wsSus.Cell(1, 4).Value = "Дата рождения";
                wsSus.Cell(1, 5).Value = "Фигурировал в других делах";
                var r = 2;
                foreach (var s in (full.Suspects ?? new System.Collections.Generic.List<Suspect>()))
                {
                    wsSus.Cell(r, 1).Value = s.LastName;
                    wsSus.Cell(r, 2).Value = s.FirstName;
                    wsSus.Cell(r, 3).Value = s.Patronymic ?? string.Empty;
                    wsSus.Cell(r, 4).Value = s.DateOfBirth?.ToString("yyyy-MM-dd") ?? string.Empty;
                    wsSus.Cell(r, 5).Value = s.AppearedInOtherCasesText ?? string.Empty;
                    r++;
                }
                wsSus.Columns().AdjustToContents();

                var wsEvi = wb.Worksheets.Add("Улики");
                wsEvi.Cell(1, 1).Value = "Тег";
                wsEvi.Cell(1, 2).Value = "Описание";
                wsEvi.Cell(1, 3).Value = "Собрано";
                wsEvi.Cell(1, 4).Value = "Собрал (ФИО)";
                wsEvi.Cell(1, 5).Value = "Собрал (Email)";
                wsEvi.Cell(1, 6).Value = "Собрал (Телефон)";
                r = 2;
                foreach (var ev in (full.EvidenceItems ?? new System.Collections.Generic.List<Evidence>()))
                {
                    wsEvi.Cell(r, 1).Value = ev.Tag;
                    wsEvi.Cell(r, 2).Value = ev.Description;
                    wsEvi.Cell(r, 3).Value = ev.CollectedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
                    wsEvi.Cell(r, 4).Value = ev.CollectedBy?.FullName ?? string.Empty;
                    wsEvi.Cell(r, 5).Value = ev.CollectedBy?.Email ?? string.Empty;
                    wsEvi.Cell(r, 6).Value = ev.CollectedBy?.Phone ?? string.Empty;
                    r++;
                }
                wsEvi.Columns().AdjustToContents();

                var wsHist = wb.Worksheets.Add("История статусов");
                wsHist.Cell(1, 1).Value = "СтатусId";
                wsHist.Cell(1, 2).Value = "Статус";
                wsHist.Cell(1, 3).Value = "Дата изменения";
                r = 2;
                foreach (var h in (full.StatusHistories ?? new System.Collections.Generic.List<CaseStatusHistory>()).OrderBy(x => x.ChangedAt))
                {
                    wsHist.Cell(r, 1).Value = h.StatusId;
                    wsHist.Cell(r, 2).Value = ((CaseStatus)h.StatusId).ToString();
                    wsHist.Cell(r, 3).Value = h.ChangedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                    r++;
                }
                wsHist.Columns().AdjustToContents();

                wb.SaveAs(fullPath);

                MessageBox.Show($"Экспорт завершён:\n{fullPath}", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string? FindProjectRoot()
        {
            try
            {
                var dir = new DirectoryInfo(AppContext.BaseDirectory!);
                for (var i = 0; i < 10 && dir != null; i++)
                {
                    var csproj = dir.GetFiles("*.csproj").FirstOrDefault();
                    if (csproj != null) return dir.FullName;
                    dir = dir.Parent;
                }
            }
            catch
            {
            }
            return null;
        }
    }
}
