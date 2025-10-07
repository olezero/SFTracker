using Microsoft.EntityFrameworkCore;
using SFTracker.Data;
using SFTracker.Models;

namespace SFTracker.Services;

public class FactoryService {
	private readonly SFTrackerDbContext m_context;

	public FactoryService(SFTrackerDbContext context) {
		m_context = context;
	}

	public async Task<List<Factory>> GetAllFactoriesAsync() {
		return await m_context.Factories
			.Include(f => f.Inputs)
				.ThenInclude(i => i.Part)
				.OrderBy(i => i.Name)
			.Include(f => f.Outputs)
				.ThenInclude(o => o.Part)
				.OrderBy(i => i.Name)
			.OrderBy(f => f.Sorting)
			.AsSplitQuery()
			.ToListAsync();
	}

	public async Task<Factory?> GetFactoryByIdAsync(int id) {
		return await m_context.Factories
			.Include(f => f.Inputs)
				.ThenInclude(i => i.Part)
				.OrderBy(i => i.Name)
			.Include(f => f.Outputs)
				.ThenInclude(o => o.Part)
				.OrderBy(o => o.Name)
			.AsSplitQuery()
			.FirstOrDefaultAsync(f => f.Id == id);
	}

	public async Task<Factory> CreateFactoryAsync(string name) {
		var factory = new Factory { Name = name };
		m_context.Factories.Add(factory);
		await m_context.SaveChangesAsync();
		return factory;
	}

	public async Task<Factory> UpdateFactoryAsync(Factory factory) {
		factory.UpdatedAt = DateTime.UtcNow;
		m_context.Factories.Update(factory);
		await m_context.SaveChangesAsync();
		return factory;
	}

	public async Task DeleteFactoryAsync(int id) {
		var factory = await m_context.Factories.FindAsync(id);
		if (factory != null) {
			m_context.Factories.Remove(factory);
			await m_context.SaveChangesAsync();
		}
	}

	public async Task AddFactoryInputAsync(int factoryId, int partId, double amountPerMinute) {
		var input = new FactoryInput {
			FactoryId = factoryId,
			PartId = partId,
			AmountPerMinute = amountPerMinute
		};
		m_context.FactoryInputs.Add(input);
		await m_context.SaveChangesAsync();
	}

	public async Task AddFactoryOutputAsync(int factoryId, int partId, double amountPerMinute) {
		var output = new FactoryOutput {
			FactoryId = factoryId,
			PartId = partId,
			AmountPerMinute = amountPerMinute
		};
		m_context.FactoryOutputs.Add(output);
		await m_context.SaveChangesAsync();
	}

	public async Task RemoveFactoryInputAsync(int inputId) {
		var input = await m_context.FactoryInputs.FindAsync(inputId);
		if (input != null) {
			m_context.FactoryInputs.Remove(input);
			await m_context.SaveChangesAsync();
		}
	}

	public async Task RemoveFactoryOutputAsync(int outputId) {
		var output = await m_context.FactoryOutputs.FindAsync(outputId);
		if (output != null) {
			m_context.FactoryOutputs.Remove(output);
			await m_context.SaveChangesAsync();
		}
	}

	/// <summary>
	/// Calculate the global resource surplus/deficit for all parts
	/// </summary>
	public async Task<Dictionary<string, double>> CalculateGlobalResourceBalanceAsync() {
		var balance = new Dictionary<string, double>();

		// Get all factories with their inputs and outputs
		var factories = await GetAllFactoriesAsync();

		foreach (var factory in factories) {
			// Subtract inputs (consumption)
			foreach (var input in factory.Inputs) {
				if (!balance.ContainsKey(input.Part.Name)) {
					balance[input.Part.Name] = 0;
				}

				balance[input.Part.Name] -= input.AmountPerMinute;
			}

			// Add outputs (production)
			foreach (var output in factory.Outputs) {
				if (!balance.ContainsKey(output.Part.Name)) {
					balance[output.Part.Name] = 0;
				}

				balance[output.Part.Name] += output.AmountPerMinute;
			}
		}

		return balance;
	}

	public async Task<List<DbPart>> GetAllPartsAsync() {
		return await m_context.Parts
			.OrderBy(p => p.Name)
			.ToListAsync();
	}
}
