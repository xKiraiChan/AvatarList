using KiraiMod.Core;
using KiraiMod.Core.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using VRC.Core;

namespace AvatarList
{
    public class AvatarList
    {
        public static readonly List<AvatarList> All = new();

        private static readonly MethodInfo m_GetComponent = typeof(Component).GetMethod(nameof(Component.GetComponent), new Type[0]);
        private static readonly MethodInfo m_Render = UiVRCList.Type.GetMethods().FirstOrDefault(x => x.ContainsGenericParameters && x.ReturnType == typeof(void)).MakeGenericMethod(typeof(ApiAvatar));
        private static readonly PropertyInfo m_ApiAvatar = SimpleAvatarPedestal.Type.GetProperties().FirstOrDefault(x => x.PropertyType == typeof(ApiAvatar));

        public static event Action OnReady;
        public static bool Ready;

        public static Transform list;
        public static GameObject change;
        public static object pageAvatar;
        public static ActivationListener activation;

        static AvatarList() => SetupDelayed().Start();

        private static IEnumerator SetupDelayed()
        {
            GameObject UserInterface;
            while ((UserInterface = GameObject.Find("UserInterface")) == null) yield return null;

            Transform screen = UserInterface.transform.Find("MenuContent/Screens/Avatar").transform;
            list = screen.Find("Vertical Scroll View/Viewport/Content/Public Avatar List");
            change = screen.Find("Change Button").gameObject;
            
            activation = screen.gameObject.AddComponent<ActivationListener>();

            pageAvatar = m_GetComponent.MakeGenericMethod(PageAvatar.Type).Invoke(screen, null);

            Ready = true;
            OnReady?.StableInvoke();
        }

        public readonly string name;
        public readonly Text label;
        public readonly object uilist;

        public List<ApiAvatar> avatars = new();

        public AvatarList(string name)
        {
            this.name = StripOrdering(name);

            if (System.IO.File.Exists($"BepInEx/config/AvatarLists/{name}.list"))
                avatars = System.IO.File.ReadAllLines($"BepInEx/config/AvatarLists/{name}.list").Select(x => {
                    string[] parts = x.Split('\t');
                    return new ApiAvatar() { id = parts[0], thumbnailImageUrl = parts[1], name = string.Join('\t', parts.Skip(2)) };
                }).ToList();

            GameObject favorites = UnityEngine.Object.Instantiate(list.gameObject, list.parent, false);
            favorites.transform.SetSiblingIndex(0);
            favorites.active = true;
            favorites.name = "AvatarList - " + this.name;

            UiAvatarList.Type.GetProperties().First(x => x.PropertyType.IsEnum && x.PropertyType.DeclaringType == UiAvatarList.Type).SetValue(
                m_GetComponent.MakeGenericMethod(UiAvatarList.Type).Invoke(favorites.transform, null),
                4 // SpecificList
            );

            uilist = m_GetComponent.MakeGenericMethod(UiVRCList.Type).Invoke(favorites.transform, null);
            UiVRCList.Type.GetProperty("clearUnseenListOnCollapse").SetValue(uilist, false);
            UiVRCList.Type.GetProperty("usePagination").SetValue(uilist, false);
            UiVRCList.Type.GetProperty("hideElementsWhenContracted").SetValue(uilist, false);
            UiVRCList.Type.GetProperty("hideWhenEmpty").SetValue(uilist, false);
            ((GameObject)UiVRCList.Type.GetProperty("pickerPrefab").GetValue(uilist)).transform.Find("TitleText").GetComponent<Text>().supportRichText = true;
            
            Transform button = favorites.transform.Find("Button");

            label = favorites.transform.Find("Button").GetComponentInChildren<Text>();
            label.text = this.name;

            GameObject favorite = UnityEngine.Object.Instantiate(change, button, false);

            RectTransform rect = favorite.GetComponent<RectTransform>();
            rect.sizeDelta = new(65, 65);
            rect.anchoredPosition = new(885, 0);

            Text favoriteText = favorite.GetComponentInChildren<Text>();
            favoriteText.text = "\u00B1";
            favoriteText.fontSize = 42;
            favoriteText.alignByGeometry = true;

            Button favoriteButton = favorite.GetComponent<Button>();
            favoriteButton.onClick = new();
            favoriteButton.On(() =>
            {
                object pedestal = null;
                for (int i = 0; i < PageAvatar.Pedestal.Length && pedestal == null; i++)
                    pedestal = PageAvatar.Pedestal[i].GetValue(pageAvatar);

                ApiAvatar avatar = (ApiAvatar)m_ApiAvatar.GetValue(pedestal);

                // .Remove wasn't working
                bool set = false;
                for (int i = 0; i < avatars.Count; i++)
                    if (avatars[i].id == avatar.id)
                    {
                        set = true;
                        avatars.RemoveAt(i);
                        break;
                    }

                if (!set)
                    if (Input.GetKey(KeyCode.LeftShift))
                        avatars.Add(avatar);
                    else avatars.Insert(0, avatar);

                System.IO.File.WriteAllLines($"BepInEx/config/AvatarLists/{name}.list", avatars.Select(x => $"{x.id}\t{x.thumbnailImageUrl}\t{x.name}"));

                Refresh();
            });

            activation.OnActivate += () => DelayedRefresh().Start();
        }

        public void Refresh()
        {
            label.text = $"{name} ({avatars.Count})";

            Il2CppSystem.Collections.Generic.List<ApiAvatar> il2cppAvatars = new(avatars.Count);
            foreach (ApiAvatar avatar in avatars)
                il2cppAvatars.Add(avatar);

            m_Render.Invoke(uilist, new object[4] { il2cppAvatars, 0, true, null });
        }
       
        private readonly WaitForSeconds Wait = new(1);
        private IEnumerator DelayedRefresh()
        {
            yield return Wait;

            Refresh();
        }

        private static string StripOrdering(string x)
        {
            char[] chars = x.ToCharArray();
            if (char.IsDigit(chars[0]) && char.IsDigit(chars[1]) && chars[2] == '_')
                return x[3..];
            else return x;
        } 
    }
}
