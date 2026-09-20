#!/usr/bin/env python3
"""Validate resource syntax and cross-resource squad references without Unity."""
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / 'Assets/Resources'

def read(path):
    return json.loads(path.read_text(encoding='utf-8'))

def main():
    assets = list(RES.rglob('*.json'))
    for path in assets:
        read(path)
    config = read(RES / 'Battles/st_squad_demo.json')
    strings = read(RES / 'Localization/strings_zh.json')['entries']
    keys = {entry['key'] for entry in strings}
    assert len(keys) == len(strings), 'Duplicate localization keys'
    tips = read(RES / 'Tips/tips_zh.json')['entries']
    tip_ids = {tip['id'] for tip in tips}
    assert len(tip_ids) == len(tips), 'Duplicate tip ids'
    party = {actor['id']: actor for actor in config['party']}
    abilities = {ability['id']: ability for ability in config['abilities']}
    assert len(party) == len(config['party']), 'Duplicate party id'
    assert len(abilities) == len(config['abilities']), 'Duplicate ability id'
    assert config['initialControlledId'] in party
    for actor in party.values():
        for ability in actor['abilities']:
            assert abilities[ability]['ownerId'] == actor['id'], ability
    for template in config['scanTemplates']:
        assert all(n > 0 for n in template['sizeMeters'])
        if template.get('attachToId'):
            assert template['attachToId'] in party
    for clue in config['interactions']:
        assert clue['tipId'] in tip_ids, clue['tipId']
    for resource in config['audio'].values():
        assert (RES / (resource + '.wav')).is_file(), resource
    def check_keys(value):
        if isinstance(value, dict):
            for k, v in value.items():
                if k.endswith('Key'):
                    assert v in keys, f'Missing localization: {v}'
                check_keys(v)
        elif isinstance(value, list):
            for item in value:
                check_keys(item)
    check_keys(config)
    for story_id in config['storyIds']:
        story = read(RES / 'Story' / (story_id + '.json'))
        assert story['id'] == story_id
        commands = story['commands']
        for cmd in commands:
            if cmd['type'] == 'tip':
                assert cmd['tip'] in tip_ids, cmd['tip']
            for option in cmd.get('options', []):
                assert 0 <= option['jumpIndex'] < len(commands)
    print(f'OK: {len(assets)} JSON resources, {len(party)} bodies, '
          f'{len(abilities)} abilities, {len(keys)} localized strings, story/tip/audio references')

if __name__ == '__main__':
    main()
