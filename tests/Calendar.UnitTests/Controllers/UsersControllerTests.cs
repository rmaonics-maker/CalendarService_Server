using AutoFixture;
using AutoMapper;
using Calendar_Service.Services;
using Calendar.Contracts;
using FluentValidation;

namespace Calendar.UnitTests.Controllers;

public class UsersControllerTests
{
    private readonly ICalendarService _service;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateUserRequest> _validator;
    private readonly IFixture _fixture;

    
}