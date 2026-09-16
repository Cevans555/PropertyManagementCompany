using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Web.Models.Reviews;

public class NoteFormViewModel
{
    [BindNever]
    public int? Id { get; set; }

    [BindNever]
    public int ApplicationId { get; set; }

    [Required]
    [StringLength(ManagerNote.TextMaxLength)]
    [Display(Name = "Note")]
    public string Text { get; set; } = string.Empty;
}
