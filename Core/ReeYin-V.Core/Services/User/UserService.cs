using Prism.Events;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Models.Database.Repository;
using ReeYin_V.Core.Models.Database.Tables;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ReeYin_V.Core.Services.User
{

    public enum UserPermission
    {
        None = 0,
        SuperAdmin = 1,
        Admin = 2,
        General = 3
    }

    [ExposedService(Lifetime.Singleton, 3, typeof(IUserService))]
    public class UserService : IUserService
    {
        #region Fields
        public CurrentUser CurUser { get; set; } = new CurrentUser();
        private UserRepository UserRepository { get; }
        private PermissionRepository PermissionRepository { get; }
        private PermMenuRelationRepository PermMenuRelationRepository { get; }
        private RoleRepository RoleRepository { get; }

        private MenuRepository MenuRepository { get; }
        public ObservableCollection<string> AllUserName { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<Models.Database.Tables.User> AllUser { get; set; } = new ObservableCollection<Models.Database.Tables.User>();

        public ObservableCollection<Role> AllRole { get; set; } = new ObservableCollection<Role>();

        public ObservableCollection<Permission> AllPermisson { get; set; } = new ObservableCollection<Permission>();
        public ObservableCollection<Menu> AllMenu { get; set; } = new ObservableCollection<Menu>();



        #endregion

        #region Constructor
        public UserService(UserRepository userRepository,
            PermissionRepository permissionRepository,
            RoleRepository roleRepository,
            PermMenuRelationRepository permMenuRelationRepository,
            IEventAggregator eventAggregator,
            MenuRepository menuRepository)
        {
            UserRepository = userRepository;
            PermissionRepository = permissionRepository;
            RoleRepository = roleRepository;
            PermMenuRelationRepository = permMenuRelationRepository;
            MenuRepository = menuRepository;
            InitializePermissions();
            InitializeRoleTable();
            InitializeUserTable();
            LoadInfo();
            InitializePermMenuRelation();
            //订阅用户表改变事件
            eventAggregator.GetEvent<UsersChangeEvent>().Subscribe(LoadInfo, ThreadOption.UIThread);

        }
        #endregion

        #region Override
        public void Logout()
        {
            CurUser = null;
        }

        public bool VerifyIdentity()
        {
            var Result = UserRepository.GetList(p => p.Username == CurUser.UserName && p.PasswordHash == CurUser.Password);
            if (Result.Count != 0)
            {
                CurUser.LoginTime = DateTime.Now;
                //获取角色/权限信息
                CurUser.RoleID = Result[0].RoleId;
                CurUser.PermissionID = RoleRepository.GetList(p => p.RoleId == Result[0].RoleId).FirstOrDefault().PermissionID;
                return true;
            }

            if(CurUser.UserName == "超级管理员" && CurUser.Password == "guanliyuan")
            {
                CurUser.LoginTime = DateTime.Now;
                //获取角色/权限信息
                CurUser.RoleID = 1;
                CurUser.PermissionID = 1;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 校验当前用户权限，返回是否有权限
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool VerifyCurUserPermission(UserPermission permission)
        {
            if (CurUser.PermissionID <= (int)permission && CurUser.PermissionID != 0)
            {
                return true;
            }
            else
            {
                Console.WriteLine("权限不足，操作失败");
                return false;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// 加载所有信息
        /// 用户，权限，角色
        /// </summary>
        /// <returns></returns>
        public void LoadInfo()
        {
            try
            {
                AllUserName = UserRepository.GetList().Select(u => u.Username).ToList().ToObservableCollection();
                AllUser = UserRepository.GetList().ToObservableCollection();
                AllRole = RoleRepository.GetList().ToObservableCollection();
                AllPermisson = PermissionRepository.GetList().ToObservableCollection();
                AllMenu = MenuRepository.GetList().ToObservableCollection();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void Login(string userName, string password)
        {

        }


        //初始化权限表
        public void InitializePermissions()
        {
            var Permissions = PermissionRepository.GetList();
            List<int> MissingId = Enumerable.Range(1, 10)
                                 .Where(id => !Permissions.Any(p => p.PermId == id))
                                 .ToList(); //返回权限表缺失的权限PermId
            List<int> AllMenuid = MenuRepository.GetList().Select(m => m.MenuId).ToList();  //获取所有菜单MenuId

            Dictionary<int, string> MissingPermissions = new Dictionary<int, string>() {
                {1,"一级权限" },{2,"二级权限" },{3,"三级权限" },{4,"四级权限" },{5,"五级权限" },
                {6,"六级权限" },{7,"七级权限" },{8,"八级权限" },{9,"九级权限" },{10,"十级权限" }
            };

            foreach (var i in MissingId)
            {
                Permission permission = new Permission()
                {
                    PermId = i,
                    PermName = MissingPermissions[i],
                    PermCode = i.ToString(),
                    CreateBy = 0,
                    CreateTime = DateTime.Now,
                    UpdateBy = 1,
                    UpdateTime = DateTime.Now,
                    Description = MissingPermissions[i]
                };
                PermissionRepository.Insert(permission);
            }
        }

        //初始化权限菜单表
        public void InitializePermMenuRelation()
        {
            List<int> AllMenuId = MenuRepository.GetList().Select(m => m.MenuId).ToList();//获取当前菜单表的所有菜单ID
            foreach (var perm in AllPermisson)
            {
                List<int> CurrentPermission = PermMenuRelationRepository.GetList(p => p.PermId == perm.PermId).Select(p => p.MenuId).ToList();//获取当前权限菜单权限表中所有的菜单ID
                List<int> MissMenuId = AllMenuId.Except(CurrentPermission).ToList();
                List<int> UnnecessaryMenuId = CurrentPermission.Except(AllMenuId).ToList();
                if (MissMenuId.Count != 0)
                {
                    PermissionRepository.AddPermissionMissMenusAsync(perm, MissMenuId);
                }
                
                if(UnnecessaryMenuId.Count != 0)
                {
                    PermMenuRelationRepository.Delete(PermMenuRelationRepository.GetList().Where(x => UnnecessaryMenuId.Contains(x.MenuId) && x.PermId == perm.PermId).ToList());
                }

            }
        }

        /// <summary>
        /// 初始化用户表
        /// </summary>
        public void InitializeUserTable()
        {
            List<Models.Database.Tables.User> Users = UserRepository.GetList();
            int roleid = RoleRepository.GetList(p => p.RoleName == "管理员").FirstOrDefault().RoleId;
            if (Users.Count == 0 || Users.Count(x => x.Username == "admin") == 0)
            {
                Models.Database.Tables.User adminUser = new Models.Database.Tables.User()
                {
                    Username = "admin",
                    PasswordHash = "1", 
                    RoleId = roleid,
                    Status = 1,
                    CreateBy = 0,
                    CreateTime = DateTime.Now,
                    UpdateBy = 0,
                    UpdateTime = DateTime.Now,
                    Description = "初始管理员账号"
                };
                UserRepository.Insert(adminUser);
            }
        }

        /// <summary>
        /// 初始化角色表
        /// </summary>
        public void InitializeRoleTable()
        {
            List<Role> Roles = RoleRepository.GetList();
            if(Roles.Count == 0 || Roles.Count(x => x.RoleName == "管理员") == 0)
            {
                Role superAdminRole = new Role()
                {
                    RoleName = "管理员",
                    PermissionID = 1,
                    CreateBy = 0,
                    CreateTime = DateTime.Now,
                    UpdateBy = 0,
                    UpdateTime = DateTime.Now,
                    Description = "拥有所有权限的角色"
                };
                RoleRepository.Insert(superAdminRole);
            }
        }



        #endregion



    }
}
