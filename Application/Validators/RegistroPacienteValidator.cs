using Domain;
using FluentValidation;
using System;

namespace Application.Validators;

public class RegistroPacienteValidator : AbstractValidator<RegistroPacienteRequestDto>
{
    public RegistroPacienteValidator()
    {
        RuleFor(x => x.Correo)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo es inválido.");

        RuleFor(x => x.Contrasena)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");

        RuleFor(x => x.TipoDocumento)
            .NotEmpty().WithMessage("El tipo de documento es obligatorio.");

        RuleFor(x => x.NumeroDocumento)
            .NotEmpty().WithMessage("El número de documento es obligatorio.")
            .Matches("^[0-9]+$").WithMessage("El número de documento solo debe contener números.");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.");

        RuleFor(x => x.Apellido)
            .NotEmpty().WithMessage("El apellido es obligatorio.");

        RuleFor(x => x.FechaNacimiento)
            .LessThan(DateOnly.FromDateTime(DateTime.Now)).WithMessage("La fecha de nacimiento debe ser una fecha pasada.");
    }
}