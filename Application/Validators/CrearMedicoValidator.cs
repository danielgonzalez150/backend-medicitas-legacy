using Domain.DTOs;
using FluentValidation;

namespace Application.Validators;

public class CrearMedicoValidator : AbstractValidator<CrearMedicoRequestDto>
{
    public CrearMedicoValidator()
    {
        RuleFor(x => x.Correo)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo es inválido.");

        RuleFor(x => x.Contrasena)
            .NotEmpty().WithMessage("La contraseña temporal es obligatoria.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");

        RuleFor(x => x.IdEspecialidad)
            .GreaterThan(0).WithMessage("Debe seleccionar una especialidad válida.");

        RuleFor(x => x.TarjetaProfesional)
            .NotEmpty().WithMessage("La tarjeta profesional es obligatoria.");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.");

        RuleFor(x => x.Apellido)
            .NotEmpty().WithMessage("El apellido es obligatorio.");
    }
}