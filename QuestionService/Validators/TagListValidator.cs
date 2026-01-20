using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace QuestionService.Validators
{
    public class TagListValidator : ValidationAttribute
    {
        private int _min {  get; set; }
        private int _max { get; set; }

        public TagListValidator(int min, int max ) 
        {
            _min = min;
            _max = max;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {

            if (value is List<string> tags) 
            { 
                if( tags.Count >= _min && tags.Count <= _max )
                { 
                    return ValidationResult.Success;
                }
                else 
                {
                    return new ValidationResult($"The number of tags must be between {_min} and {_max}. Current count: {tags.Count}");
                }   
            }
            else 
            {
                return new ValidationResult("Invalid tag list.");
            }   
        }
    }
}
