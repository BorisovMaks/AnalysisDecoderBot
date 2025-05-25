using AnalysisDecoderBot.Enums;
using System;
using Telegram.Bot.Types;

namespace AnalysisDecoderBot.Models
{
    public class UserModel : ModelBase
    {
        public UserModel()
        {

        }

        public UserModel(
            long chatId,
            string name,
            GenderEnum gender,
            float weight,
            float height,
            int age,
            bool isAdministrator = false)
        {
            ChatId = chatId;
            Name = name;
            Gender = gender;
            Weight = weight;
            Height = height;
            Age = age;
            IsAdministrator = isAdministrator;
        }

        public string Name { get; private set; }
        public void SetName(string name)
        {
            Name = name;
        }

        public GenderEnum Gender { get; private set; }
        public void SetGender(GenderEnum gender)
        {
            Gender = gender;
        }

        public float Weight { get; private set; }
        public void SetWeight(float weight)
        {
            Weight = weight;
        }

        public float Height { get; private set; }
        public void SetHeight(float height)
        {
            Height = height;
        }

        public int Age { get; private set; }
        public void SetAge(int age)
        {
            Age = age;
        }
        
        public long ChatId { get; private set; }
        public void SetChatId(long chatId)
        {
            ChatId = chatId;
        }

        public bool IsAdministrator { get; }

        public override string ToString()
        {
            return string.Format("Информация о пользователь '{0}'. Пол '{1}'. Вес '{2}'. Рост '{3}'. Возраст '{4}'.", 
                Name, 
                Gender.GetDescription(), 
                Weight, 
                Height, 
                Age);
        }
    }
}
