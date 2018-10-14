using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using TaskManagement.Data.Service;
using System.Windows.Data;
using System.ComponentModel;
using TaskManagement.Data;
using Caliburn.Micro;
using AutoMapper;
using UiModel = TaskManagement.Model.Model;
using DataModel = TaskManagement.Data.Model;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows;
using TaskManagement.Model.Model;
using TaskManagement.Data.IServices;
using ToastNotifications;
using System.Timers;
using Notifications.Wpf;

namespace TaskManagement.ViewModels
{
    public class TaskManagerViewModel : BaseViewModel, IHandle<ITaskService>, IHandle<KeyValuePair<TaskState, UiModel.Task>>, IHandle<UiModel.Task>, IHandle<int>
    {
        private ITaskService TaskService;

        public List<UiModel.Task> RawTasks { get; set; }

        private BindableCollection<UiModel.Task> newTasks;

        public BindableCollection<UiModel.Task> NewTasks
        {
            get { return newTasks; }
            set
            {
                newTasks = value;
                NotifyOfPropertyChange(() => NewTasks);
            }
        }

        private BindableCollection<UiModel.Task> tasksInProgress;

        public BindableCollection<UiModel.Task> TasksInProgress
        {
            get { return tasksInProgress; }
            set
            {
                tasksInProgress = value;
                NotifyOfPropertyChange(() => TasksInProgress);
            }
        }

        private BindableCollection<UiModel.Task> tasksComplted;

        public BindableCollection<UiModel.Task> TasksCompleted
        {
            get { return tasksComplted; }
            set
            {
                tasksComplted = value;
                NotifyOfPropertyChange(() => TasksCompleted);
            }
        }

        public UiModel.Task selectedTask;

        public UiModel.Task SelectedTask
        {
            get { return selectedTask; }
            set
            {
                selectedTask = value;
                NotifyOfPropertyChange(() => SelectedTask);
            }
        }

        private string dateDescription;

        public string DateDescription
        {
            get { return dateDescription; }
            set
            {
                dateDescription = value;
                NotifyOfPropertyChange(() => DateDescription);
            }
        }

        private DateTime fromDate = DateTime.Today.AddMonths(-1);

        public DateTime FromDate
        {
            get { return fromDate; }
            set
            {
                fromDate = value;
                NotifyOfPropertyChange(() => FromDate);
            }
        }

        private DateTime toDate = DateTime.Now;

        public DateTime ToDate
        {
            get { return toDate; }
            set
            {
                toDate = value;
                NotifyOfPropertyChange(() => ToDate);
            }
        }

        private UiModel.Task rawTask;

        public UiModel.Task RawTask
        {
            get { return rawTask; }
            set
            {
                rawTask = value;
                NotifyOfPropertyChange(() => RawTask);
            }
        }

        public TaskManagerViewModel()
        {
            if (TaskService != null)
            {
                LoadTasks();
            }
        }

        private void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            var notificationManager = new NotificationManager();
            var items = RawTasks.Where(t => t.Status == Status.New || t.Status == Status.InProgress);
            int count = 1;
            foreach (var item in items)
            {
                bool canShow = false;
                if (item.DueDate.Date <= DateTime.Now.AddDays(3))
                    canShow = true;
                if (canShow)
                {
                    notificationManager.Show(new NotificationContent
                    {
                        Title = item.Name,
                        //Message = $"Due Date: {item.DueDate}",
                        Message = GetNotificationMessage(item.DueDate),
                        Type = TaskNotificataion(item.DueDate),

                    }, "", TimeSpan.FromSeconds(count++ * 3));
                }
            }
        }

        private string GetNotificationMessage(DateTime dueDate)
        {
            if (dueDate.Date.Date < DateTime.Now.Date)
                return $"You task is overdue - {dueDate.ToString("dddd, dd MMMM yyyy")}";
            else if (dueDate.Date.Date.ToShortDateString() == DateTime.Now.Date.ToShortDateString())
                return $"Your task is due today @{dueDate.ToString("hh:mm tt")}";
            else if (dueDate.Date.Date < DateTime.Now.AddDays(2).Date)
                return $"Your task is due tomorrow @{dueDate.ToString("hh:mm tt")}";
            else if (dueDate.Date.Date < DateTime.Now.AddDays(3).Date)
                return $"Your task to be completed by {dueDate.ToString("dddd, hh:mm tt")}";
            return string.Empty;
        }

        private NotificationType TaskNotificataion(DateTime dueDate)
        {
            if (dueDate.Date.Date < DateTime.Now.Date)
                return NotificationType.Error;
            else if (dueDate.Date.Date.ToShortDateString() == DateTime.Now.Date.ToShortDateString())
                return NotificationType.Success;
            else if (dueDate.Date.Date < DateTime.Now.AddDays(2).Date)
                return NotificationType.Warning;
            else if (dueDate.Date.Date < DateTime.Now.AddDays(3).Date)
                return NotificationType.Information;
            return NotificationType.Information;
        }

        public void LoadTasks()
        {
            RawTasks = Mapper.Map<List<UiModel.Task>>(TaskService.GetAll());
            NewTasks = GetTasks(Status.New);
            TasksInProgress = GetTasks(Status.InProgress);
            TasksCompleted = GetTasks(Status.Completed);
        }

        private BindableCollection<UiModel.Task> GetTasks(Status status)
        {
            return new BindableCollection<UiModel.Task>(RawTasks.Where(t => t.Status == status).Take(Helper.Constants.TotalTasks));
        }

        public void TasksArcheiveView()
        {
            eventAggregator.BeginPublishOnUIThread(IoC.Get<TaskArchiveViewModel>());
            eventAggregator.BeginPublishOnUIThread(TaskService);
        }

        public void CreateTask(UiModel.Task task)
        {
            task.Id = GetId();
            task.CompletedDate = null;
            switch (task.Status)
            {
                case Status.New:
                    NewTasks.Add(task);
                    break;
                case Status.InProgress:
                    TasksInProgress.Add(task);
                    break;
                case Status.Completed:
                    task.CompletedDate = DateTime.Now;
                    TasksCompleted.Add(task);
                    break;
            }
        }

        public void EditTask(UiModel.Task task)
        {
            switch (task.Status)
            {
                case Status.New:
                    UiModel.Task newTask = NewTasks.FirstOrDefault(t => t.Id == task.Id);
                    if (newTask != null)
                        Mapper.Map(task, newTask);
                    else
                    {
                        DeleteFromList(RawTask);
                        task.CompletedDate = null;
                        NewTasks.Add(task);
                    }
                    break;
                case Status.InProgress:
                    UiModel.Task inProgressTask = TasksInProgress.FirstOrDefault(t => t.Id == task.Id);
                    if (inProgressTask != null)
                        Mapper.Map(task, inProgressTask);
                    else
                    {
                        DeleteFromList(RawTask);
                        task.CompletedDate = null;
                        TasksInProgress.Add(task);
                    }
                    break;
                case Status.Completed:
                    UiModel.Task completedTask = TasksCompleted.FirstOrDefault(t => t.Id == task.Id);
                    if (completedTask != null)
                        Mapper.Map(task, completedTask);
                    else
                    {
                        DeleteFromList(RawTask);
                        task.CompletedDate = DateTime.Now;
                        TasksCompleted.Add(task);
                    }
                    break;
            }
        }

        public void DeleteFromList(UiModel.Task task)
        {
            switch (task.Status)
            {
                case Status.New:
                    NewTasks.Remove(NewTasks.FirstOrDefault(t => t.Id == task.Id));
                    break;
                case Status.InProgress:
                    TasksInProgress.Remove(TasksInProgress.FirstOrDefault(t => t.Id == task.Id));
                    break;
                case Status.Completed:
                    TasksCompleted.Remove(TasksCompleted.FirstOrDefault(t => t.Id == task.Id));
                    break;
            }
        }

        public void DragTask(EventArgs eventArgs)
        {
            if (SelectedTask != null)
            {
                MouseEventArgs mouseArgs = (MouseEventArgs)eventArgs;
                if (mouseArgs.LeftButton == MouseButtonState.Pressed)
                {
                    DataObject dragData = new DataObject(Helper.Constants.SelectedTask, SelectedTask);
                    DragDrop.DoDragDrop((DependencyObject)mouseArgs.Source, dragData, DragDropEffects.Move);
                }
            }
        }

        public void DropTask(DragEventArgs e)
        {
            string name = ((DependencyObject)e.Source).GetValue(FrameworkElement.NameProperty) as string;
            if (e.Data.GetDataPresent(Helper.Constants.SelectedTask))
            {
                UiModel.Task task = e.Data.GetData(Helper.Constants.SelectedTask) as UiModel.Task;
                DragAndDrop(name, task);
            }
        }

        public void DragAndDrop(String name, UiModel.Task task)
        {
            switch (name)
            {
                case nameof(NewTasks):
                    if (task.Status != Status.New)
                    {
                        DeleteFromList(task);
                        task.Status = Status.New;
                        task.CompletedDate = null;
                        NewTasks.Add(task);
                        TaskService.Update(Mapper.Map<DataModel.Task>(task));
                    }
                    break;
                case nameof(TasksInProgress):
                    if (task.Status != Status.InProgress)
                    {
                        DeleteFromList(task);
                        task.CompletedDate = null;
                        task.Status = Status.InProgress;
                        TasksInProgress.Add(task);
                        TaskService.Update(Mapper.Map<DataModel.Task>(task));
                    }
                    break;
                case nameof(TasksCompleted):
                    if (task.Status != Status.Completed)
                    {
                        DeleteFromList(task);
                        task.CompletedDate = DateTime.Now;
                        task.Status = Status.Completed;
                        TasksCompleted.Add(task);
                        TaskService.Update(Mapper.Map<DataModel.Task>(task));
                    }
                    break;
            }
        }

        public void DeleteTask()
        {
            if (MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                DeleteTask(SelectedTask);
            }
        }

        public void Handle(ITaskService message)
        {
            TaskService = message;
            t.Stop();
            t.Elapsed -= T_Elapsed;
            t.Elapsed += T_Elapsed;
            t.Start();
            LoadTasks();
        }

        public void Handle(KeyValuePair<TaskState, UiModel.Task> message)
        {
            switch (message.Key)
            {
                case TaskState.New:
                    CreateTask(message.Value);
                    TaskService.Add(Mapper.Map<DataModel.Task>(message.Value));
                    break;
                case TaskState.Update:
                    EditTask(message.Value);
                    TaskService.Update(Mapper.Map<DataModel.Task>(message.Value));
                    break;
            }
        }

        public void DeleteTask(UiModel.Task task)
        {
            switch (task.Status)
            {
                case Status.New:
                    NewTasks.Remove(NewTasks.FirstOrDefault(t => t.Id == task.Id));
                    TaskService.Delete(task.Id);
                    break;
                case Status.InProgress:
                    TasksInProgress.Remove(TasksInProgress.FirstOrDefault(t => t.Id == task.Id));
                    TaskService.Delete(task.Id);
                    break;
                case Status.Completed:
                    TasksCompleted.Remove(TasksCompleted.FirstOrDefault(t => t.Id == task.Id));
                    TaskService.Delete(task.Id);
                    break;
            }
        }

        public void SetEditTask()
        {
            RawTask = SelectedTask;
            eventAggregator.BeginPublishOnUIThread(RawTask);
        }

        public Guid GetId()
        {
            return Guid.NewGuid();
        }

        public void Handle(UiModel.Task message)
        {
            RawTask = message;
        }

        public void DisplayTasksArchieve()
        {
            eventAggregator.PublishOnUIThread(IoC.Get<TaskArchiveViewModel>());
            eventAggregator.PublishOnUIThread(TaskService);
        }

        public void MainView()
        {
            eventAggregator.PublishOnUIThread(IoC.Get<MainViewModel>());
            eventAggregator.PublishOnUIThread(TaskService);
        }

        public Notifier Notifier;
        Timer t = new Timer(1000 * 3600);

        public void Handle(int message)
        {
            t.Interval = 1000 * 60 * message;
        }
    }
}